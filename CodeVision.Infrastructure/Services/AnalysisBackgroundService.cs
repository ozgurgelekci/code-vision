using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CodeVision.Core.Entities;
using CodeVision.Core.Interfaces;
using CodeVision.Core.Models;

namespace CodeVision.Infrastructure.Services;

public class AnalysisBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalysisBackgroundService> _logger;
    private readonly IAnalysisQueue _queue;
    
    public AnalysisBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AnalysisBackgroundService> logger,
        IAnalysisQueue queue)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                if (job != null)
                {
                    await ProcessAnalysisJobAsync(job);
                }
            }
            catch (OperationCanceledException)
            {
                // Service is stopping, normal condition
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background service processing error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Analysis Background Service stopping");
    }

    private async Task ProcessAnalysisJobAsync(AnalysisJob job)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            _logger.LogInformation("Starting PR analysis: {JobId} - {Repo} #{PrNumber}",
                job.Id, job.RepoName, job.PrNumber);

            var analysisService = scope.ServiceProvider.GetRequiredService<IPullRequestAnalysisService>();
            var roslynService = scope.ServiceProvider.GetRequiredService<IRoslynAnalyzerService>();
            var gptService = scope.ServiceProvider.GetRequiredService<IGptAnalysisService>();
            // var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Get analysis record
            var analysis = await analysisService.GetAnalysisAsync(job.AnalysisId);
            if (analysis == null)
            {
                await _queue.FailJobAsync(job.Id, "Analysis record not found");
                return;
            }

            // Update status
            analysis.Status = AnalysisStatus.InProgress;
            await analysisService.UpdateAnalysisAsync(analysis);
            
            // Send started notification
            // await notificationService.SendAnalysisStartedAsync(analysis.Id);

            // No mock diff content; real provider should supply diff content in future
            string diffContent = string.Empty;

            // Run Roslyn analysis
            var roslynFindings = await roslynService.AnalyzePullRequestDiffAsync(diffContent);
            analysis.SetRoslynFindings(roslynFindings);

            // Determine risk level
            analysis.RiskLevel = await roslynService.DetermineRiskLevelAsync(roslynFindings);

            // Run GPT analysis
            var summary = await gptService.GenerateSummaryAsync(diffContent, job.PrTitle);
            analysis.Summary = summary;

            var gptSuggestions = await gptService.AnalyzePotentialIssuesAsync(diffContent);
            analysis.SetGptSuggestions(gptSuggestions);

            // Calculate quality score
            var roslynScore = await roslynService.CalculateQualityScoreAsync(roslynFindings);
            var gptScore = await gptService.CalculateGptQualityScoreAsync(gptSuggestions);
            analysis.QualityScore = (int)((roslynScore * 0.6) + (gptScore * 0.4)); // 60% Roslyn, 40% GPT

            // Complete analysis
            analysis.Status = AnalysisStatus.Completed;
            analysis.ProcessedAt = DateTime.UtcNow;
            
            await analysisService.UpdateAnalysisAsync(analysis);
            await _queue.CompleteJobAsync(job.Id);

            // Send completed notification via SignalR hub
            try
            {
                var hub = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<CodeVision.Infrastructure.Hubs.AnalysisNotificationHub>>();
                await hub.Clients.All.SendCoreAsync("AnalysisCompleted", new object[] { analysis.Id }, default);
            }
            catch (Exception hubEx)
            {
                _logger.LogWarning(hubEx, "AnalysisCompleted hub notification failed");
            }

            // High risk check
            if (analysis.RiskLevel == RiskLevel.High)
            {
                var criticalFindings = roslynFindings.Where(f => f.Severity == Severity.Error).ToList();
                // await notificationService.SendHighRiskDetectedAsync(analysis.Id, criticalFindings);
            }

            _logger.LogInformation("PR analysis completed: {JobId} - Score: {Score}, Risk: {Risk}",
                job.Id, analysis.QualityScore, analysis.RiskLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PR analysis: {JobId}", job.Id);
            await _queue.FailJobAsync(job.Id, ex.Message);
            
            // Update analysis status and send failure notification
            try
            {
                var analysisService = scope.ServiceProvider.GetRequiredService<IPullRequestAnalysisService>();
                // var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var analysis = await analysisService.GetAnalysisAsync(job.AnalysisId);
                if (analysis != null)
                {
                    analysis.Status = AnalysisStatus.Failed;
                    await analysisService.UpdateAnalysisAsync(analysis);
                    // await notificationService.SendAnalysisFailedAsync(analysis.Id, ex.Message);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Error while updating analysis status: {JobId}", job.Id);
            }
        }
    }
}

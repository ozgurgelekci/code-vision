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
        _logger.LogInformation("Analysis Background Service başlatıldı");

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
                // Service durduruluyor, normal durum
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background service işlem hatası");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Analysis Background Service durduruluyor");
    }

    private async Task ProcessAnalysisJobAsync(AnalysisJob job)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            _logger.LogInformation("PR analizi başlatılıyor: {JobId} - {Repo} #{PrNumber}",
                job.Id, job.RepoName, job.PrNumber);

            var analysisService = scope.ServiceProvider.GetRequiredService<IPullRequestAnalysisService>();
            var roslynService = scope.ServiceProvider.GetRequiredService<IRoslynAnalyzerService>();
            var gptService = scope.ServiceProvider.GetRequiredService<IGptAnalysisService>();
            // var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Analiz kaydını al
            var analysis = await analysisService.GetAnalysisAsync(job.AnalysisId);
            if (analysis == null)
            {
                await _queue.FailJobAsync(job.Id, "Analiz kaydı bulunamadı");
                return;
            }

            // Durumu güncelle
            analysis.Status = AnalysisStatus.InProgress;
            await analysisService.UpdateAnalysisAsync(analysis);
            
            // Başlangıç bildirimi gönder
            // await notificationService.SendAnalysisStartedAsync(analysis.Id);

            // Mock diff content kullan (GitHub service yok)
            string diffContent = CreateMockDiffContent(job);

            // Roslyn analizi yap
            var roslynFindings = await roslynService.AnalyzePullRequestDiffAsync(diffContent);
            analysis.SetRoslynFindings(roslynFindings);

            // Risk seviyesini belirle
            analysis.RiskLevel = await roslynService.DetermineRiskLevelAsync(roslynFindings);

            // GPT analizi yap
            var summary = await gptService.GenerateSummaryAsync(diffContent, job.PrTitle);
            analysis.Summary = summary;

            var gptSuggestions = await gptService.AnalyzePotentialIssuesAsync(diffContent);
            analysis.SetGptSuggestions(gptSuggestions);

            // Kalite puanını hesapla
            var roslynScore = await roslynService.CalculateQualityScoreAsync(roslynFindings);
            var gptScore = await gptService.CalculateGptQualityScoreAsync(gptSuggestions);
            analysis.QualityScore = (int)((roslynScore * 0.6) + (gptScore * 0.4)); // %60 Roslyn, %40 GPT

            // Analizi tamamla
            analysis.Status = AnalysisStatus.Completed;
            analysis.ProcessedAt = DateTime.UtcNow;
            
            await analysisService.UpdateAnalysisAsync(analysis);
            await _queue.CompleteJobAsync(job.Id);

            // Tamamlanma bildirimi gönder
            // await notificationService.SendAnalysisCompletedAsync(analysis.Id, analysis.QualityScore, analysis.RiskLevel);

            // Yüksek risk kontrolü
            if (analysis.RiskLevel == RiskLevel.High)
            {
                var criticalFindings = roslynFindings.Where(f => f.Severity == Severity.Error).ToList();
                // await notificationService.SendHighRiskDetectedAsync(analysis.Id, criticalFindings);
            }

            _logger.LogInformation("PR analizi tamamlandı: {JobId} - Score: {Score}, Risk: {Risk}",
                job.Id, analysis.QualityScore, analysis.RiskLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PR analizi sırasında hata oluştu: {JobId}", job.Id);
            await _queue.FailJobAsync(job.Id, ex.Message);
            
            // Analiz durumunu güncelle ve hata bildirimi gönder
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
                _logger.LogError(updateEx, "Analiz durumu güncellenirken hata oluştu: {JobId}", job.Id);
            }
        }
    }

    private static string CreateMockDiffContent(AnalysisJob job)
    {
        return $@"diff --git a/src/Example.cs b/src/Example.cs
index 1234567..abcdefg 100644
--- a/src/Example.cs
+++ b/src/Example.cs
@@ -1,10 +1,15 @@
 using System;
 
+// PR: {job.PrTitle} by {job.PrAuthor}
 namespace Example
 {{
     public class ExampleClass
     {{
+        private readonly string _connectionString = ""Data Source=localhost;Initial Catalog=test;"";
+        
         public void ProcessData()
         {{
+            // TODO: Implement proper error handling
+            var result = DoSomething();
             Console.WriteLine(""Processing data..."");
         }}
     }}
}}";
    }
}

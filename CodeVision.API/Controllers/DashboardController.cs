using Microsoft.AspNetCore.Mvc;
using CodeVision.Core.Interfaces;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IPullRequestAnalysisService _analysisService;
    private readonly IAnalysisQueue _analysisQueue;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IPullRequestAnalysisService analysisService,
        IAnalysisQueue analysisQueue,
        ILogger<DashboardController> logger)
    {
        _analysisService = analysisService;
        _analysisQueue = analysisQueue;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var qualityMetrics = await _analysisService.GetQualityMetricsAsync();
            var recentAnalyses = await _analysisService.GetAnalysesAsync(0, 5);
            var queueLength = await _analysisQueue.GetQueueLengthAsync();

            return Ok(new
            {
                overview = new
                {
                    totalAnalyses = qualityMetrics["total"],
                    averageQualityScore = qualityMetrics["average_score"],
                    highQualityCount = qualityMetrics["high_quality"],
                    mediumQualityCount = qualityMetrics["medium_quality"],
                    lowQualityCount = qualityMetrics["low_quality"],
                    pendingAnalyses = queueLength
                },
                riskDistribution = new
                {
                    high = qualityMetrics["high_risk"],
                    medium = qualityMetrics["medium_risk"],
                    low = qualityMetrics["low_risk"]
                },
                qualityDistribution = new
                {
                    excellent = qualityMetrics["high_quality"],
                    good = qualityMetrics["medium_quality"],
                    needsImprovement = qualityMetrics["low_quality"]
                },
                recentAnalyses = recentAnalyses.Select(a => new
                {
                    id = a.Id,
                    repoName = a.RepoName,
                    prNumber = a.PrNumber,
                    prTitle = a.PrTitle,
                    prAuthor = a.PrAuthor,
                    status = a.Status.ToString(),
                    qualityScore = a.QualityScore,
                    riskLevel = a.RiskLevel.ToString(),
                    processedAt = a.ProcessedAt
                }),
                recentNotifications = new List<object>(),
                systemStatus = new
                {
                    queueLength,
                    isHealthy = queueLength < 50,
                    lastUpdate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
using CodeVision.Core.Entities;

namespace CodeVision.Core.Interfaces;

public interface INotificationService
{
    Task SendAnalysisStartedAsync(string analysisId);
    Task SendAnalysisCompletedAsync(string analysisId, int qualityScore, RiskLevel riskLevel);
    Task SendAnalysisFailedAsync(string analysisId, string errorMessage);
    Task SendHighRiskDetectedAsync(string analysisId, List<RoslynFinding> criticalFindings);
}

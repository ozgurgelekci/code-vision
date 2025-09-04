using CodeVision.Core.Entities;

namespace CodeVision.Core.Interfaces;

public interface INotificationService
{
    Task SendAnalysisStartedAsync(Guid analysisId);
    Task SendAnalysisCompletedAsync(Guid analysisId, int qualityScore, RiskLevel riskLevel);
    Task SendAnalysisFailedAsync(Guid analysisId, string errorMessage);
    Task SendHighRiskDetectedAsync(Guid analysisId, List<RoslynFinding> criticalFindings);
    Task<List<AnalysisNotification>> GetNotificationsAsync(int skip = 0, int take = 50);
    Task<bool> MarkAsReadAsync(Guid notificationId);
}

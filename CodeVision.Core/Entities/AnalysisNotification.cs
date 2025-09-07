namespace CodeVision.Core.Entities;

public class AnalysisNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PullRequestAnalysisId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual PullRequestAnalysis PullRequestAnalysis { get; set; } = null!;
}

public enum NotificationType
{
    AnalysisStarted = 0,
    AnalysisCompleted = 1,
    AnalysisFailed = 2,
    HighRiskDetected = 3
}

namespace CodeVision.Core.Entities;

public class AnalysisNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PullRequestAnalysisId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    
    // Navigation property
    public virtual PullRequestAnalysis PullRequestAnalysis { get; set; } = null!;
}

public enum NotificationType
{
    AnalysisStarted = 0,
    AnalysisInProgress = 1,
    AnalysisCompleted = 2,
    AnalysisFailed = 3,
    HighRiskDetected = 4
}

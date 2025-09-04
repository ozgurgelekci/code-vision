namespace CodeVision.Core.Entities;

public class AnalysisNotification
{
    public Guid Id { get; set; }
    public Guid PullRequestAnalysisId { get; set; }
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

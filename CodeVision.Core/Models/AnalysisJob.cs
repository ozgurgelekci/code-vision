namespace CodeVision.Core.Models;

public class AnalysisJob
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public string RepoName { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string PrTitle { get; set; } = string.Empty;
    public string PrAuthor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public AnalysisJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum AnalysisJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Retrying = 4
}

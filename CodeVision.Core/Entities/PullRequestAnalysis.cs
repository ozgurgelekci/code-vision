using System.Text.Json;

namespace CodeVision.Core.Entities;

public class PullRequestAnalysis
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RepoName { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string PrTitle { get; set; } = string.Empty;
    public string PrAuthor { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int QualityScore { get; set; }
    public string RoslynFindings { get; set; } = string.Empty; // JSON olarak saklanacak
    public string GptSuggestions { get; set; } = string.Empty; // JSON olarak saklanacak
    public RiskLevel RiskLevel { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? RawDiff { get; set; } // Opsiyonel, sıkıştırılmış olarak saklanabilir
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<AnalysisNotification> Notifications { get; set; } = new List<AnalysisNotification>();

    // Helper methods
    public List<RoslynFinding> GetRoslynFindings()
    {
        if (string.IsNullOrEmpty(RoslynFindings))
            return new List<RoslynFinding>();
            
        return JsonSerializer.Deserialize<List<RoslynFinding>>(RoslynFindings) ?? new List<RoslynFinding>();
    }

    public void SetRoslynFindings(List<RoslynFinding> findings)
    {
        RoslynFindings = JsonSerializer.Serialize(findings);
    }

    public List<GptSuggestion> GetGptSuggestions()
    {
        if (string.IsNullOrEmpty(GptSuggestions))
            return new List<GptSuggestion>();
            
        return JsonSerializer.Deserialize<List<GptSuggestion>>(GptSuggestions) ?? new List<GptSuggestion>();
    }

    public void SetGptSuggestions(List<GptSuggestion> suggestions)
    {
        GptSuggestions = JsonSerializer.Serialize(suggestions);
    }
}

public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum AnalysisStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

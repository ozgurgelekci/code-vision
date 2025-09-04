namespace CodeVision.Core.Entities;

public class GptSuggestion
{
    public string Type { get; set; } = string.Empty; // "potential_issue", "refactor", "performance", "security"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string CodeSnippet { get; set; } = string.Empty;
    public string? SuggestedCode { get; set; }
    public Priority Priority { get; set; }
    public string Category { get; set; } = string.Empty; // "code_quality", "performance", "security", "maintainability"
    public int ImpactScore { get; set; } // 1-10 arası etki puanı
    public List<string> Tags { get; set; } = new();
}

public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

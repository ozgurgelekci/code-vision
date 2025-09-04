using CodeVision.Core.Entities;

namespace CodeVision.Core.Interfaces;

public interface IGptAnalysisService
{
    Task<string> GenerateSummaryAsync(string diffContent, string prTitle);
    Task<List<GptSuggestion>> GenerateRefactorSuggestionsAsync(string codeSnippet, string fileName);
    Task<List<GptSuggestion>> AnalyzePotentialIssuesAsync(string diffContent);
    Task<List<GptSuggestion>> AnalyzeSecurityConcernsAsync(string codeSnippet);
    Task<int> CalculateGptQualityScoreAsync(List<GptSuggestion> suggestions);
}

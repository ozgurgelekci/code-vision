using CodeVision.Core.Entities;

namespace CodeVision.Core.Interfaces;

public interface IRoslynAnalyzerService
{
    Task<List<RoslynFinding>> AnalyzeCodeAsync(string codeContent, string fileName);
    Task<List<RoslynFinding>> AnalyzePullRequestDiffAsync(string diffContent);
    Task<int> CalculateQualityScoreAsync(List<RoslynFinding> findings);
    Task<RiskLevel> DetermineRiskLevelAsync(List<RoslynFinding> findings);
}

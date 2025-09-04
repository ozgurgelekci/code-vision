using CodeVision.Core.Entities;

namespace CodeVision.Core.Interfaces;

public interface IPullRequestAnalysisService
{
    Task<PullRequestAnalysis> CreateAnalysisAsync(string repoName, int prNumber, string prTitle, string prAuthor);
    Task<PullRequestAnalysis?> GetAnalysisAsync(Guid id);
    Task<PullRequestAnalysis?> GetAnalysisByPrAsync(string repoName, int prNumber);
    Task<List<PullRequestAnalysis>> GetAnalysesAsync(int skip = 0, int take = 50);
    Task<PullRequestAnalysis> UpdateAnalysisAsync(PullRequestAnalysis analysis);
    Task<bool> DeleteAnalysisAsync(Guid id);
    Task<List<PullRequestAnalysis>> GetAnalysesByRepoAsync(string repoName, int skip = 0, int take = 50);
    Task<Dictionary<string, int>> GetQualityMetricsAsync();
}

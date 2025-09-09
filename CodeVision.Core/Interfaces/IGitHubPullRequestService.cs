namespace CodeVision.Core.Interfaces;

public interface IGitHubPullRequestService
{
    Task<string> GetPullRequestDiffAsync(string repoFullName, int prNumber, CancellationToken cancellationToken = default);
}



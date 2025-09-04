using CodeVision.Core.Models;

namespace CodeVision.Core.Interfaces;

public interface IAnalysisQueue
{
    Task EnqueueAsync(AnalysisJob job);
    Task<AnalysisJob?> DequeueAsync(CancellationToken cancellationToken = default);
    Task CompleteJobAsync(string jobId);
    Task FailJobAsync(string jobId, string errorMessage);
    Task<List<AnalysisJob>> GetPendingJobsAsync(int count = 10);
    Task<int> GetQueueLengthAsync();
}

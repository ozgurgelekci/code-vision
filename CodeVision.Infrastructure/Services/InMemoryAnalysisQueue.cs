using System.Collections.Concurrent;
using CodeVision.Core.Interfaces;
using CodeVision.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeVision.Infrastructure.Services;

public class InMemoryAnalysisQueue : IAnalysisQueue
{
    private readonly ConcurrentQueue<AnalysisJob> _queue = new();
    private readonly ConcurrentDictionary<string, AnalysisJob> _jobsInProgress = new();
    private readonly ConcurrentDictionary<string, AnalysisJob> _completedJobs = new();
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly ILogger<InMemoryAnalysisQueue> _logger;

    public InMemoryAnalysisQueue(ILogger<InMemoryAnalysisQueue> logger)
    {
        _logger = logger;
    }

    public async Task EnqueueAsync(AnalysisJob job)
    {
        job.CreatedAt = DateTime.UtcNow;
        job.Status = AnalysisJobStatus.Pending;
        
        _queue.Enqueue(job);
        _semaphore.Release();
        
        _logger.LogInformation("Analiz işi kuyruğa eklendi: {JobId} - {Repo} PR #{PrNumber}",
            job.Id, job.RepoName, job.PrNumber);
        
        await Task.CompletedTask;
    }

    public async Task<AnalysisJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        if (_queue.TryDequeue(out var job))
        {
            job.Status = AnalysisJobStatus.Processing;
            _jobsInProgress[job.Id] = job;
            
            _logger.LogInformation("Analiz işi kuyruktan alındı: {JobId} - {Repo} PR #{PrNumber}",
                job.Id, job.RepoName, job.PrNumber);
            
            return job;
        }

        return null;
    }

    public async Task CompleteJobAsync(string jobId)
    {
        if (_jobsInProgress.TryRemove(jobId, out var job))
        {
            job.Status = AnalysisJobStatus.Completed;
            _completedJobs[jobId] = job;
            
            _logger.LogInformation("Analiz işi tamamlandı: {JobId} - {Repo} PR #{PrNumber}",
                job.Id, job.RepoName, job.PrNumber);
        }
        
        await Task.CompletedTask;
    }

    public async Task FailJobAsync(string jobId, string errorMessage)
    {
        if (_jobsInProgress.TryRemove(jobId, out var job))
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorMessage = errorMessage;
            job.RetryCount++;
            
            // 3 defadan fazla hata olursa retry yapma
            if (job.RetryCount < 3)
            {
                job.Status = AnalysisJobStatus.Retrying;
                
                // 5 saniye sonra tekrar kuyruğa ekle
                _ = Task.Delay(TimeSpan.FromSeconds(5))
                    .ContinueWith(_ => EnqueueAsync(job));
                
                _logger.LogWarning("Analiz işi tekrar denenecek: {JobId} - Retry: {RetryCount}",
                    job.Id, job.RetryCount);
            }
            else
            {
                _completedJobs[jobId] = job;
                _logger.LogError("Analiz işi başarısız: {JobId} - {Error}",
                    job.Id, errorMessage);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task<List<AnalysisJob>> GetPendingJobsAsync(int count = 10)
    {
        var pendingJobs = _queue.ToArray().Take(count).ToList();
        return await Task.FromResult(pendingJobs);
    }

    public async Task<int> GetQueueLengthAsync()
    {
        return await Task.FromResult(_queue.Count);
    }

    // Cleanup completed jobs periodically
    public void CleanupOldJobs()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24); // 24 saat öncesini temizle
        
        var oldJobs = _completedJobs.Values
            .Where(j => j.CreatedAt < cutoff)
            .Select(j => j.Id)
            .ToList();

        foreach (var jobId in oldJobs)
        {
            _completedJobs.TryRemove(jobId, out _);
        }
    }
}

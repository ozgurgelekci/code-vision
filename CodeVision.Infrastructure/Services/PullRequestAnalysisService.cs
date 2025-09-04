using Microsoft.EntityFrameworkCore;
using CodeVision.Core.Entities;
using CodeVision.Core.Interfaces;
using CodeVision.Infrastructure.Data;

namespace CodeVision.Infrastructure.Services;

public class PullRequestAnalysisService : IPullRequestAnalysisService
{
    private readonly IRepository<PullRequestAnalysis> _repository;
    private readonly CodeVisionDbContext _context;

    public PullRequestAnalysisService(IRepository<PullRequestAnalysis> repository, CodeVisionDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<PullRequestAnalysis> CreateAnalysisAsync(string repoName, int prNumber, string prTitle, string prAuthor)
    {
        // Mevcut analiz var mı kontrol et
        var existingAnalysis = await _repository.FirstOrDefaultAsync(a => a.RepoName == repoName && a.PrNumber == prNumber);
        if (existingAnalysis != null)
        {
            // Mevcut analizi güncelle
            existingAnalysis.PrTitle = prTitle;
            existingAnalysis.PrAuthor = prAuthor;
            existingAnalysis.Status = AnalysisStatus.Pending;
            await _repository.UpdateAsync(existingAnalysis);
            return existingAnalysis;
        }

        var analysis = new PullRequestAnalysis
        {
            Id = Guid.NewGuid().ToString(),
            RepoName = repoName,
            PrNumber = prNumber,
            PrTitle = prTitle,
            PrAuthor = prAuthor,
            Status = AnalysisStatus.Pending,
            QualityScore = 0,
            RiskLevel = RiskLevel.Low,
            ProcessedAt = DateTime.UtcNow,
            RoslynFindings = "[]", // Empty JSON array for PostgreSQL jsonb
            GptSuggestions = "[]", // Empty JSON array for PostgreSQL jsonb
            Summary = "", // Initialize summary
            RawDiff = "" // Initialize raw diff
        };

        return await _repository.AddAsync(analysis);
    }

    public async Task<PullRequestAnalysis?> GetAnalysisAsync(string id)
    {
        return await _context.PullRequestAnalyses
            .Include(a => a.Notifications)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<PullRequestAnalysis?> GetAnalysisByPrAsync(string repoName, int prNumber)
    {
        return await _context.PullRequestAnalyses
            .Include(a => a.Notifications)
            .FirstOrDefaultAsync(a => a.RepoName == repoName && a.PrNumber == prNumber);
    }

    public async Task<List<PullRequestAnalysis>> GetAnalysesAsync(int skip = 0, int take = 50)
    {
        return await _context.PullRequestAnalyses
            .Include(a => a.Notifications)
            .OrderByDescending(a => a.ProcessedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<PullRequestAnalysis> UpdateAnalysisAsync(PullRequestAnalysis analysis)
    {
        await _repository.UpdateAsync(analysis);
        return analysis;
    }

    public async Task<bool> DeleteAnalysisAsync(string id)
    {
        var analysis = await _repository.GetByIdAsync(id);
        if (analysis == null)
            return false;

        await _repository.DeleteAsync(analysis);
        return true;
    }

    public async Task<List<PullRequestAnalysis>> GetAnalysesByRepoAsync(string repoName, int skip = 0, int take = 50)
    {
        return await _context.PullRequestAnalyses
            .Include(a => a.Notifications)
            .Where(a => a.RepoName == repoName)
            .OrderByDescending(a => a.ProcessedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetQualityMetricsAsync()
    {
        var totalAnalyses = await _context.PullRequestAnalyses.CountAsync();
        
        // Eğer hiç analiz yoksa default değerleri döndür
        if (totalAnalyses == 0)
        {
            return new Dictionary<string, int>
            {
                ["total"] = 0,
                ["high_quality"] = 0,
                ["medium_quality"] = 0,
                ["low_quality"] = 0,
                ["average_score"] = 0,
                ["high_risk"] = 0,
                ["medium_risk"] = 0,
                ["low_risk"] = 0
            };
        }

        var highQualityCount = await _context.PullRequestAnalyses.CountAsync(a => a.QualityScore >= 90);
        var mediumQualityCount = await _context.PullRequestAnalyses.CountAsync(a => a.QualityScore >= 70 && a.QualityScore < 90);
        var lowQualityCount = await _context.PullRequestAnalyses.CountAsync(a => a.QualityScore < 70);
        var averageScore = await _context.PullRequestAnalyses.AverageAsync(a => a.QualityScore);

        return new Dictionary<string, int>
        {
            ["total"] = totalAnalyses,
            ["high_quality"] = highQualityCount,
            ["medium_quality"] = mediumQualityCount,
            ["low_quality"] = lowQualityCount,
            ["average_score"] = (int)Math.Round(averageScore),
            ["high_risk"] = await _context.PullRequestAnalyses.CountAsync(a => a.RiskLevel == RiskLevel.High),
            ["medium_risk"] = await _context.PullRequestAnalyses.CountAsync(a => a.RiskLevel == RiskLevel.Medium),
            ["low_risk"] = await _context.PullRequestAnalyses.CountAsync(a => a.RiskLevel == RiskLevel.Low)
        };
    }
}

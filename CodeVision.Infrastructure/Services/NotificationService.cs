using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CodeVision.Core.Entities;
using CodeVision.Core.Interfaces;
using CodeVision.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using CodeVision.Infrastructure.Hubs;

namespace CodeVision.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly CodeVisionDbContext _context;
    private readonly IHubContext<AnalysisNotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        CodeVisionDbContext context,
        IHubContext<AnalysisNotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendAnalysisStartedAsync(string analysisId)
    {
        try
        {
            var analysis = await _context.PullRequestAnalyses.FindAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analiz bulunamadı: {AnalysisId}", analysisId);
                return;
            }

            var notification = new AnalysisNotification
            {
                Id = Guid.NewGuid().ToString(),
                PullRequestAnalysisId = analysisId,
                Message = $"PR #{analysis.PrNumber} analizi başladı - {analysis.RepoName}",
                Type = NotificationType.AnalysisStarted,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.AnalysisNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // SignalR ile bildirim gönder
            var groupName = $"analysis_{analysisId}";
            var repoGroupName = $"repo_{analysis.RepoName.Replace("/", "_")}";

            await _hubContext.Clients.Groups(groupName, repoGroupName, "all_users")
                .SendAsync("AnalysisStarted", new
                {
                    analysisId = analysisId.ToString(),
                    repoName = analysis.RepoName,
                    prNumber = analysis.PrNumber,
                    prTitle = analysis.PrTitle,
                    prAuthor = analysis.PrAuthor,
                    message = notification.Message,
                    timestamp = notification.CreatedAt
                });

            _logger.LogInformation("Analiz başlangıç bildirimi gönderildi: {AnalysisId}", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz başlangıç bildirimi hatası: {AnalysisId}", analysisId);
        }
    }

    public async Task SendAnalysisCompletedAsync(string analysisId, int qualityScore, RiskLevel riskLevel)
    {
        try
        {
            var analysis = await _context.PullRequestAnalyses.FindAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analiz bulunamadı: {AnalysisId}", analysisId);
                return;
            }

            var notification = new AnalysisNotification
            {
                Id = Guid.NewGuid().ToString(),
                PullRequestAnalysisId = analysisId,
                Message = $"PR #{analysis.PrNumber} analizi tamamlandı - Skor: {qualityScore}, Risk: {riskLevel}",
                Type = NotificationType.AnalysisCompleted,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.AnalysisNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // SignalR ile bildirim gönder
            var groupName = $"analysis_{analysisId}";
            var repoGroupName = $"repo_{analysis.RepoName.Replace("/", "_")}";

            await _hubContext.Clients.Groups(groupName, repoGroupName, "all_users")
                .SendAsync("AnalysisCompleted", new
                {
                    analysisId = analysisId.ToString(),
                    repoName = analysis.RepoName,
                    prNumber = analysis.PrNumber,
                    prTitle = analysis.PrTitle,
                    prAuthor = analysis.PrAuthor,
                    qualityScore,
                    riskLevel = riskLevel.ToString(),
                    message = notification.Message,
                    timestamp = notification.CreatedAt
                });

            _logger.LogInformation("Analiz tamamlanma bildirimi gönderildi: {AnalysisId}", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz tamamlanma bildirimi hatası: {AnalysisId}", analysisId);
        }
    }

    public async Task SendAnalysisFailedAsync(string analysisId, string errorMessage)
    {
        try
        {
            var analysis = await _context.PullRequestAnalyses.FindAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analiz bulunamadı: {AnalysisId}", analysisId);
                return;
            }

            var notification = new AnalysisNotification
            {
                Id = Guid.NewGuid().ToString(),
                PullRequestAnalysisId = analysisId,
                Message = $"PR #{analysis.PrNumber} analizi başarısız - Hata: {errorMessage}",
                Type = NotificationType.AnalysisFailed,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.AnalysisNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // SignalR ile bildirim gönder
            var groupName = $"analysis_{analysisId}";
            var repoGroupName = $"repo_{analysis.RepoName.Replace("/", "_")}";

            await _hubContext.Clients.Groups(groupName, repoGroupName, "all_users")
                .SendAsync("AnalysisFailed", new
                {
                    analysisId = analysisId.ToString(),
                    repoName = analysis.RepoName,
                    prNumber = analysis.PrNumber,
                    prTitle = analysis.PrTitle,
                    prAuthor = analysis.PrAuthor,
                    errorMessage,
                    message = notification.Message,
                    timestamp = notification.CreatedAt
                });

            _logger.LogError("Analiz başarısızlık bildirimi gönderildi: {AnalysisId} - {Error}", analysisId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz başarısızlık bildirimi hatası: {AnalysisId}", analysisId);
        }
    }

    public async Task SendHighRiskDetectedAsync(string analysisId, List<RoslynFinding> criticalFindings)
    {
        try
        {
            var analysis = await _context.PullRequestAnalyses.FindAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analiz bulunamadı: {AnalysisId}", analysisId);
                return;
            }

            var criticalCount = criticalFindings.Count(f => f.Severity == Severity.Error);
            var notification = new AnalysisNotification
            {
                Id = Guid.NewGuid().ToString(),
                PullRequestAnalysisId = analysisId,
                Message = $"YÜKSEK RİSK: PR #{analysis.PrNumber} - {criticalCount} kritik sorun tespit edildi",
                Type = NotificationType.HighRiskDetected,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.AnalysisNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // SignalR ile bildirim gönder
            var groupName = $"analysis_{analysisId}";
            var repoGroupName = $"repo_{analysis.RepoName.Replace("/", "_")}";

            await _hubContext.Clients.Groups(groupName, repoGroupName, "all_users")
                .SendAsync("HighRiskDetected", new
                {
                    analysisId = analysisId.ToString(),
                    repoName = analysis.RepoName,
                    prNumber = analysis.PrNumber,
                    prTitle = analysis.PrTitle,
                    prAuthor = analysis.PrAuthor,
                    criticalIssuesCount = criticalCount,
                    criticalFindings = criticalFindings.Take(5).Select(f => new
                    {
                        ruleId = f.RuleId,
                        title = f.Title,
                        message = f.Message,
                        severity = f.Severity.ToString(),
                        filePath = f.FilePath,
                        lineNumber = f.LineNumber
                    }).ToList(),
                    message = notification.Message,
                    timestamp = notification.CreatedAt
                });

            _logger.LogWarning("Yüksek risk bildirimi gönderildi: {AnalysisId} - {CriticalCount} kritik sorun", 
                analysisId, criticalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yüksek risk bildirimi hatası: {AnalysisId}", analysisId);
        }
    }

    public async Task<List<AnalysisNotification>> GetNotificationsAsync(int skip = 0, int take = 50)
    {
        return await _context.AnalysisNotifications
            .Include(n => n.PullRequestAnalysis)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        var notification = await _context.AnalysisNotifications.FindAsync(notificationId);
        if (notification == null)
            return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
}

// SignalR interface (client methods için)
public interface IAnalysisNotificationHub
{
    Task AnalysisStarted(object data);
    Task AnalysisCompleted(object data);
    Task AnalysisFailed(object data);
    Task HighRiskDetected(object data);
}

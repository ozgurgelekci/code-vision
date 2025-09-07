using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CodeVision.API.Models;
using CodeVision.Core.Interfaces;
using CodeVision.Core.Models;
using Microsoft.AspNetCore.SignalR;
using CodeVision.API.Hubs;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("webhook")]
public class GitHubWebhookController : ControllerBase
{
    private readonly ILogger<GitHubWebhookController> _logger;
    private readonly IPullRequestAnalysisService _analysisService;
    private readonly IAnalysisQueue _analysisQueue;
    private readonly IHubContext<AnalysisNotificationHub> _hubContext;
    
    public GitHubWebhookController(
        ILogger<GitHubWebhookController> logger, 
        IPullRequestAnalysisService analysisService,
        IAnalysisQueue analysisQueue,
        IHubContext<AnalysisNotificationHub> hubContext)
    {
        _logger = logger;
        _analysisService = analysisService;
        _analysisQueue = analysisQueue;
        _hubContext = hubContext;
    }

    [HttpPost("github")]
    public async Task<IActionResult> HandleGitHubWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<GitHubWebhookPayload>(payload.GetRawText());
            if (webhookPayload?.PullRequest == null)
            {
                return BadRequest("Invalid payload");
            }

            _logger.LogInformation("GitHub webhook received: {Action} - {Repo} PR #{Number}",
                webhookPayload.Action, webhookPayload.Repository.FullName, webhookPayload.PullRequest.Number);

            if (ShouldProcessPullRequest(webhookPayload.Action))
            {
                await ProcessPullRequestAsync(webhookPayload);
            }
            else if (webhookPayload.Action == "closed")
            {
                await HandlePullRequestClosedAsync(webhookPayload);
            }

            return Ok(new { message = "Webhook processed", action = webhookPayload.Action });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static bool ShouldProcessPullRequest(string action) =>
        action is "opened" or "synchronize" or "reopened";

    private async Task ProcessPullRequestAsync(GitHubWebhookPayload payload)
    {
        try
        {
            var analysis = await _analysisService.CreateAnalysisAsync(
                payload.Repository.FullName,
                payload.PullRequest.Number,
                payload.PullRequest.Title,
                payload.PullRequest.User.Login);

            var job = new AnalysisJob
            {
                Id = Guid.NewGuid().ToString(),
                AnalysisId = analysis.Id,
                RepoName = payload.Repository.FullName,
                PrNumber = payload.PullRequest.Number,
                PrTitle = payload.PullRequest.Title,
                PrAuthor = payload.PullRequest.User.Login
            };

            await _analysisQueue.EnqueueAsync(job);
            // Broadcast new PR detected
            await _hubContext.Clients.All.SendAsync("NewPullRequest", new
            {
                repo = payload.Repository.FullName,
                prNumber = payload.PullRequest.Number,
                title = payload.PullRequest.Title,
                author = payload.PullRequest.User.Login
            });
            _logger.LogInformation("Analysis job queued: {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PR analysis");
        }
    }

    private async Task HandlePullRequestClosedAsync(GitHubWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("PR closed, deleting analysis: {Repo} PR #{Number}",
                payload.Repository.FullName, payload.PullRequest.Number);

            // Find existing analysis by repo and PR number
            var existingAnalysis = await _analysisService.GetAnalysisByPrAsync(
                payload.Repository.FullName, 
                payload.PullRequest.Number);

            if (existingAnalysis != null)
            {
                var deleted = await _analysisService.DeleteAnalysisAsync(existingAnalysis.Id);
                if (deleted)
                {
                    _logger.LogInformation("Analysis deleted successfully: {AnalysisId}", existingAnalysis.Id);
                    
                    // Broadcast PR deletion to connected clients
                    await _hubContext.Clients.All.SendAsync("PullRequestClosed", new
                    {
                        repo = payload.Repository.FullName,
                        prNumber = payload.PullRequest.Number,
                        title = payload.PullRequest.Title,
                        analysisId = existingAnalysis.Id
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to delete analysis: {AnalysisId}", existingAnalysis.Id);
                }
            }
            else
            {
                _logger.LogInformation("No analysis found for closed PR: {Repo} PR #{Number}",
                    payload.Repository.FullName, payload.PullRequest.Number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PR closure: {Repo} PR #{Number}",
                payload.Repository.FullName, payload.PullRequest.Number);
        }
    }
}
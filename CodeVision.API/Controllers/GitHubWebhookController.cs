using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CodeVision.API.Models;
using CodeVision.Core.Interfaces;
using CodeVision.Core.Models;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("webhook")]
public class GitHubWebhookController : ControllerBase
{
    private readonly ILogger<GitHubWebhookController> _logger;
    private readonly IPullRequestAnalysisService _analysisService;
    private readonly IAnalysisQueue _analysisQueue;
    
    public GitHubWebhookController(
        ILogger<GitHubWebhookController> logger, 
        IPullRequestAnalysisService analysisService,
        IAnalysisQueue analysisQueue)
    {
        _logger = logger;
        _analysisService = analysisService;
        _analysisQueue = analysisQueue;
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
            _logger.LogInformation("Analysis job queued: {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PR analysis");
        }
    }
}
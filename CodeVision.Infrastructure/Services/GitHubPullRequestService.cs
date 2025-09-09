using System.Net.Http.Headers;
using CodeVision.Core.Configuration;
using CodeVision.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeVision.Infrastructure.Services;

public class GitHubPullRequestService : IGitHubPullRequestService
{
    private readonly ILogger<GitHubPullRequestService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GitHubPullRequestService(ILogger<GitHubPullRequestService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("code-vision", "1.0"));
    }

    public async Task<string> GetPullRequestDiffAsync(string repoFullName, int prNumber, CancellationToken cancellationToken = default)
    {
        var apiUrl = _configuration["GitHub:ApiUrl"] ?? "https://api.github.com";
        var token = _configuration["GitHub:Token"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var url = $"{apiUrl}/repos/{repoFullName}/pulls/{prNumber}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));

        try
        {
            var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub diff fetch failed: {Status} {Repo}#{Pr}", res.StatusCode, repoFullName, prNumber);
                return string.Empty;
            }
            return await res.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub diff fetch error for {Repo}#{Pr}", repoFullName, prNumber);
            return string.Empty;
        }
    }
}



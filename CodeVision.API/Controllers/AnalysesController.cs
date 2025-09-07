using Microsoft.AspNetCore.Mvc;
using CodeVision.Core.Interfaces;
using System.Text.RegularExpressions;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysesController : ControllerBase
{
    private readonly IPullRequestAnalysisService _analysisService;
    private readonly ILogger<AnalysesController> _logger;

    public AnalysesController(
        IPullRequestAnalysisService analysisService,
        ILogger<AnalysesController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalyses([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        try
        {
            var analyses = await _analysisService.GetAnalysesAsync(skip, take);
            return Ok(analyses.Select(a => new
            {
                id = a.Id,
                repoName = a.RepoName,
                prNumber = a.PrNumber,
                prTitle = a.PrTitle,
                prAuthor = a.PrAuthor,
                status = a.Status.ToString(),
                qualityScore = a.QualityScore,
                riskLevel = a.RiskLevel.ToString(),
                processedAt = a.ProcessedAt,
                createdAt = a.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analyses");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnalysisDetail(string id)
    {
        try
        {
            var analysis = await _analysisService.GetAnalysisByIdAsync(id);
            if (analysis == null)
            {
                return NotFound(new { error = "Analysis not found" });
            }

            // Get Roslyn findings
            var roslynFindings = await _analysisService.GetRoslynFindingsAsync(id);
            
            // Get GPT suggestions  
            var gptSuggestions = await _analysisService.GetGptSuggestionsAsync(id);

            var normalizedSummary = NormalizeSummaryToHtml(analysis.Summary ?? string.Empty);
            var result = new
            {
                id = analysis.Id,
                repoName = analysis.RepoName,
                prNumber = analysis.PrNumber,
                prTitle = analysis.PrTitle,
                prAuthor = analysis.PrAuthor,
                status = analysis.Status.ToString(),
                qualityScore = analysis.QualityScore,
                riskLevel = analysis.RiskLevel.ToString(),
                summary = SanitizeSummary(normalizedSummary),
                processedAt = analysis.ProcessedAt,
                createdAt = analysis.CreatedAt,
                roslynFindings = roslynFindings?.Select(f => (object)new
                {
                    id = f.Id,
                    ruleId = f.RuleId,
                    severity = f.Severity.ToString(),
                    message = f.Message,
                    filePath = f.FilePath,
                    lineNumber = f.LineNumber,
                    category = f.Category,
                    description = f.Description
                }).ToList() ?? new List<object>(),
                gptSuggestions = gptSuggestions?.Select(s => (object)new
                {
                    id = s.Id,
                    type = s.Type.ToString(),
                    title = s.Title,
                    description = s.Description,
                    priority = s.Priority.ToString(),
                    filePath = s.FilePath,
                    startLine = s.StartLine,
                    endLine = s.EndLine,
                    originalCode = s.OriginalCode,
                    suggestedCode = s.SuggestedCode
                }).ToList() ?? new List<object>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis detail: {AnalysisId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static string SanitizeSummary(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Remove script/style blocks
        var sanitized = Regex.Replace(html, "(?is)<(script|style)[^>]*>.*?</\\1>", string.Empty);

        // Remove inline event handlers and javascript: href/src
        sanitized = Regex.Replace(sanitized, @"on\\w+\\s*=\\s*""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"on\\w+\\s*=\\s*'[^']*'", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"on\\w+\\s*=\\s*[^\s>]+", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"(href|src)\\s*=\\s*['""]?\\s*javascript:[^'"">\s]+['""]?", string.Empty, RegexOptions.IgnoreCase);

        // Keep only allowed tags; strip attributes
        var allowed = "p|br|strong|b|em|i|u|ul|ol|li|code|pre|h1|h2|h3|h4";
        // Strip attributes from allowed tags
        sanitized = Regex.Replace(sanitized, $@"<({allowed})(?:\\s+[^>]*)?>", "<$1>", RegexOptions.IgnoreCase);
        // Remove any other tags (keep inner text)
        sanitized = Regex.Replace(sanitized, $@"</?(?!({allowed}))\\w+[^>]*>", string.Empty, RegexOptions.IgnoreCase);

        return sanitized;
    }

    private static string NormalizeSummaryToHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // If already contains list markup, return as-is
        if (input.Contains("<ol") || input.Contains("<ul"))
        {
            return input;
        }

        var lines = input.Replace("\r\n", "\n").Split('\n');
        var sb = new System.Text.StringBuilder();
        var inOl = false;

        foreach (var raw in lines)
        {
            var line = raw?.TrimEnd() ?? string.Empty;
            var numberMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+[\.)]\s+(.*)$");

            if (numberMatch.Success)
            {
                if (!inOl)
                {
                    sb.Append("<ol>");
                    inOl = true;
                }

                var content = numberMatch.Groups[1].Value.Trim();

                // If pattern like "Title: description" then move description to next line
                var colonIdx = content.IndexOf(':');
                if (colonIdx > 0 && colonIdx < 120)
                {
                    var head = content.Substring(0, colonIdx).Trim();
                    var rest = content.Substring(colonIdx + 1).Trim();
                    sb.Append("<li><strong>" + System.Net.WebUtility.HtmlEncode(head) + "</strong>");
                    if (!string.IsNullOrEmpty(rest))
                    {
                        sb.Append("<br/>" + System.Net.WebUtility.HtmlEncode(rest));
                    }
                    sb.Append("</li>");
                }
                else
                {
                    sb.Append("<li>" + System.Net.WebUtility.HtmlEncode(content) + "</li>");
                }
            }
            else
            {
                if (inOl)
                {
                    sb.Append("</ol>");
                    inOl = false;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.Append("<p>" + line + "</p>");
                }
            }
        }

        if (inOl)
        {
            sb.Append("</ol>");
        }

        return sb.ToString();
    }
}

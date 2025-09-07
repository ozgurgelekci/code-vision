using Microsoft.AspNetCore.Mvc;
using CodeVision.Core.Interfaces;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysesController : ControllerBase
{
    private readonly IPullRequestAnalysisService _analysisService;
    private readonly ILogger<AnalysesController> _logger;
    private readonly Ganss.XSS.HtmlSanitizer _sanitizer = new();

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
                summary = _sanitizer.Sanitize(analysis.Summary ?? string.Empty),
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
}

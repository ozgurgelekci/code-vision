using OpenAI.Chat;
using System.Text.Json;
using CodeVision.Core.Entities;
using CodeVision.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeVision.Infrastructure.Services;

public class GptAnalysisService : IGptAnalysisService
{
    private readonly ILogger<GptAnalysisService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ChatClient _chatClient;

    public GptAnalysisService(ILogger<GptAnalysisService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-4";
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured");
            _chatClient = null!;
        }
        else
        {
            _chatClient = new ChatClient(model, apiKey);
        }
    }

    public async Task<string> GenerateSummaryAsync(string diffContent, string prTitle)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI client is not configured, returning default summary");
            return $"PR Title: {prTitle}\nSummary: OpenAI integration is not configured.";
        }

        try
        {
            var prompt = CreateSummaryPrompt(diffContent, prTitle);
            var response = await _chatClient.CompleteChatAsync(prompt);
            
            return response.Value.Content[0].Text ?? "Summary could not be generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT summary generation error");
            return $"PR Title: {prTitle}\nSummary: An error occurred during automated analysis.";
        }
    }

    public async Task<List<GptSuggestion>> GenerateRefactorSuggestionsAsync(string codeSnippet, string fileName)
    {
        if (_chatClient == null)
        {
            return new List<GptSuggestion>();
        }

        try
        {
            var prompt = CreateRefactorPrompt(codeSnippet, fileName);
            var response = await _chatClient.CompleteChatAsync(prompt);
            var content = response.Value.Content[0].Text;

            return ParseGptSuggestions(content, fileName, SuggestionType.Refactor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT refactor suggestions generation error: {FileName}", fileName);
            return new List<GptSuggestion>();
        }
    }

    public async Task<List<GptSuggestion>> AnalyzePotentialIssuesAsync(string diffContent)
    {
        if (_chatClient == null)
        {
            return new List<GptSuggestion>();
        }

        try
        {
            var prompt = CreateIssueAnalysisPrompt(diffContent);
            var response = await _chatClient.CompleteChatAsync(prompt);
            var content = response.Value.Content[0].Text;

            return ParseGptSuggestions(content, "mixed", SuggestionType.PotentialIssue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT potential issues analysis error");
            return new List<GptSuggestion>();
        }
    }

    public async Task<List<GptSuggestion>> AnalyzeSecurityConcernsAsync(string codeSnippet)
    {
        if (_chatClient == null)
        {
            return new List<GptSuggestion>();
        }

        try
        {
            var prompt = CreateSecurityAnalysisPrompt(codeSnippet);
            var response = await _chatClient.CompleteChatAsync(prompt);
            var content = response.Value.Content[0].Text;

            return ParseGptSuggestions(content, "security", SuggestionType.Security);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT security analysis error");
            return new List<GptSuggestion>();
        }
    }

    public async Task<int> CalculateGptQualityScoreAsync(List<GptSuggestion> suggestions)
    {
        const int baseScore = 100;
        var penalty = 0;

        foreach (var suggestion in suggestions)
        {
            penalty += suggestion.Priority switch
            {
                Priority.Critical => 15,
                Priority.High => 10,
                Priority.Medium => 5,
                Priority.Low => 2,
                _ => 0
            };
        }

        var score = Math.Max(0, baseScore - penalty);
        return await Task.FromResult(score);
    }

    private string CreateSummaryPrompt(string diffContent, string prTitle)
    {
        return $@"Analyze the code changes below. First, summarize what changed in 2–3 sentences.
Then provide the following sections with concise, actionable items:

PR Title: {prTitle}

Diff:
{TruncateContent(diffContent, 4000)}

Respond in English. Structure the result using HTML headings and lists:
<h4>Summary</h4>
- 2–3 sentence summary

<h4>Potential Issues</h4>
- Numbered list of potential bugs or risks (if any)

<h4>Improvement Suggestions</h4>
- Numbered list of refactor or quality improvements";
    }

    private string CreateRefactorPrompt(string codeSnippet, string fileName)
    {
        return $@"List concrete steps to make this method or class more readable, testable, and performant.
Provide a short code example for each suggestion. Also note any risks (e.g., behavior change or compatibility).

File: {fileName}
Code:
{TruncateContent(codeSnippet, 2000)}

Return JSON with the following schema:
{{
    ""suggestions"": [
        {{
            ""type"": ""refactor"",
            ""title"": ""Suggestion title"",
            ""description"": ""Detailed explanation"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""code_quality|performance|maintainability"",
            ""suggestedCode"": ""Example code"",
            ""impactScore"": ""1-10"",
            ""tags"": [""tag1"", ""tag2""]
        }}
    ]
}}";
    }

    private string CreateIssueAnalysisPrompt(string diffContent)
    {
        return $@"Identify potential bugs, performance problems, and code quality issues in the following diff.
For each finding, provide a priority and a clear remediation suggestion.

Code:
{TruncateContent(diffContent, 3000)}

Return JSON with this schema:
{{
    ""suggestions"": [
        {{
            ""type"": ""potential_issue"",
            ""title"": ""Issue title"",
            ""description"": ""Detailed explanation"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""code_quality|performance|security|maintainability"",
            ""impactScore"": ""1-10"",
            ""tags"": [""tag1"", ""tag2""]
        }}
    ]
}}";
    }

    private string CreateSecurityAnalysisPrompt(string codeSnippet)
    {
        return $@"Find security vulnerabilities, concurrency hazards, and async/await or exception-handling issues.
Call out any race conditions, deadlock risks, or misuse of exceptions.

Code:
{TruncateContent(codeSnippet, 2000)}

Return JSON with this schema:
{{
    ""suggestions"": [
        {{
            ""type"": ""security"",
            ""title"": ""Security issue title"",
            ""description"": ""Detailed explanation and fix"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""security"",
            ""impactScore"": ""1-10"",
            ""tags"": [""security"", ""concurrency"", ""async""]
        }}
    ]
}}";
    }

    private List<GptSuggestion> ParseGptSuggestions(string? content, string defaultFileName, SuggestionType defaultType)
    {
        var suggestions = new List<GptSuggestion>();

        if (string.IsNullOrEmpty(content))
            return suggestions;

        try
        {
            // JSON formatını bulmaya çalış
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
                var result = JsonSerializer.Deserialize<GptResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Suggestions != null)
                {
                    suggestions.AddRange(result.Suggestions.Select(s => new GptSuggestion
                    {
                        Type = ParseSuggestionType(s.Type) ?? defaultType,
                        Title = s.Title ?? "GPT Suggestion",
                        Description = s.Description ?? "No description",
                        FilePath = defaultFileName,
                        Priority = ParsePriority(s.Priority),
                        Category = s.Category ?? "code_quality",
                        ImpactScore = ParseImpactScore(s.ImpactScore),
                        Tags = s.Tags ?? new List<string>(),
                        SuggestedCode = s.SuggestedCode
                    }));
                }
            }
            else
            {
                // JSON not found, create a single text suggestion
                suggestions.Add(new GptSuggestion
                {
                    Type = defaultType,
                    Title = "GPT Analysis",
                    Description = TruncateContent(content, 500),
                    FilePath = defaultFileName,
                    Priority = Priority.Medium,
                    Category = "code_quality",
                    ImpactScore = 5,
                    Tags = new List<string> { "gpt-analysis" }
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GPT response could not be parsed, using fallback");
            
            suggestions.Add(new GptSuggestion
            {
                Type = defaultType,
                Title = "GPT Analysis (Parse Error)",
                Description = TruncateContent(content, 500),
                FilePath = defaultFileName,
                Priority = Priority.Low,
                Category = "code_quality",
                ImpactScore = 3,
                Tags = new List<string> { "gpt-analysis", "parse-error" }
            });
        }

        return suggestions;
    }

    private static Priority ParsePriority(string? priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "critical" => Priority.Critical,
            "high" => Priority.High,
            "medium" => Priority.Medium,
            "low" => Priority.Low,
            _ => Priority.Medium
        };
    }

    private static int ParseImpactScore(string? impactScore)
    {
        if (string.IsNullOrEmpty(impactScore))
            return 5; // Default değer

        // String'den int'e dönüştürmeyi dene
        if (int.TryParse(impactScore, out int score))
        {
            // 1-10 arasında sınırla
            return Math.Max(1, Math.Min(10, score));
        }

        // Parse edilemezse default değer
        return 5;
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;
            
        return content[..maxLength] + "\n... (content truncated)";
    }

    private static SuggestionType? ParseSuggestionType(string? typeStr)
    {
        if (string.IsNullOrEmpty(typeStr))
            return null;

        return typeStr.ToLowerInvariant() switch
        {
            "potential_issue" => SuggestionType.PotentialIssue,
            "refactor" => SuggestionType.Refactor,
            "performance" => SuggestionType.Performance,
            "security" => SuggestionType.Security,
            "code_quality" => SuggestionType.CodeQuality,
            "best_practice" => SuggestionType.BestPractice,
            _ => null
        };
    }

    // JSON Deserialization için yardımcı sınıflar
    private class GptResponse
    {
        public List<GptSuggestionDto>? Suggestions { get; set; }
    }

    private class GptSuggestionDto
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public string? SuggestedCode { get; set; }
        public string? ImpactScore { get; set; } // String olarak al, parse sırasında int'e çevir
        public List<string>? Tags { get; set; }
    }
}

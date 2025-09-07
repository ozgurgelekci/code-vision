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
            _logger.LogWarning("OpenAI API anahtarı yapılandırılmamış");
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
            _logger.LogWarning("OpenAI client yapılandırılmamış, varsayılan özet döndürülüyor");
            return $"PR Başlığı: {prTitle}\nÖzet: OpenAI entegrasyonu yapılandırılmamış.";
        }

        try
        {
            var prompt = CreateSummaryPrompt(diffContent, prTitle);
            var response = await _chatClient.CompleteChatAsync(prompt);
            
            return response.Value.Content[0].Text ?? "Özet oluşturulamadı.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT özet oluşturma hatası");
            return $"PR Başlığı: {prTitle}\nÖzet: Otomatik analiz sırasında hata oluştu.";
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
            _logger.LogError(ex, "GPT refaktör önerileri oluşturma hatası: {FileName}", fileName);
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
            _logger.LogError(ex, "GPT potansiyel sorunlar analiz hatası");
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
            _logger.LogError(ex, "GPT güvenlik analizi hatası");
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
        return $@"Aşağıdaki kod değişikliklerini analiz et. Önce ne değiştiğini 2-3 cümlede özetle.
Sonra aşağıdaki bölümleri kısa ve uygulanabilir maddeler halinde ver:

PR Başlığı: {prTitle}

Diff:
{TruncateContent(diffContent, 12000)}

Türkçe yanıt ver. Sonucu HTML başlıkları ve listeler kullanarak yapılandır:
<h4>Özet</h4>
- 2-3 cümlelik özet

<h4>Potansiyel Sorunlar</h4>
- Potansiyel hata veya risklerin numaralı listesi (varsa)

<h4>İyileştirme Önerileri</h4>
- Refaktör veya kalite iyileştirmelerinin numaralı listesi";
    }

    private string CreateRefactorPrompt(string codeSnippet, string fileName)
    {
        return $@"Bu metodu veya sınıfı daha okunabilir, test edilebilir ve performanslı hale getirmek için somut adımları listele.
Her öneri için kısa bir kod örneği ver. Ayrıca riskleri de not et (örn. davranış değişikliği veya uyumluluk).

Dosya: {fileName}
Kod:
{TruncateContent(codeSnippet, 8000)}

Aşağıdaki şema ile JSON döndür:
{{
    ""suggestions"": [
        {{
            ""type"": ""refactor"",
            ""title"": ""Öneri başlığı"",
            ""description"": ""Detaylı açıklama"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""code_quality|performance|maintainability"",
            ""suggestedCode"": ""Örnek kod"",
            ""impactScore"": ""1-10"",
            ""tags"": [""etiket1"", ""etiket2""]
        }}
    ]
}}";
    }

    private string CreateIssueAnalysisPrompt(string diffContent)
    {
        return $@"Aşağıdaki diff'te potansiyel hataları, performans sorunlarını ve kod kalitesi sorunlarını tespit et.
Her bulgu için bir öncelik ve net bir düzeltme önerisi ver.

Kod:
{TruncateContent(diffContent, 10000)}

Bu şema ile JSON döndür:
{{
    ""suggestions"": [
        {{
            ""type"": ""potential_issue"",
            ""title"": ""Sorun başlığı"",
            ""description"": ""Detaylı açıklama"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""code_quality|performance|security|maintainability"",
            ""impactScore"": ""1-10"",
            ""tags"": [""etiket1"", ""etiket2""]
        }}
    ]
}}";
    }

    private string CreateSecurityAnalysisPrompt(string codeSnippet)
    {
        return $@"Güvenlik açıklarını, eşzamanlılık tehlikelerini ve async/await veya exception-handling sorunlarını bul.
Race condition'ları, deadlock risklerini veya exception'ların yanlış kullanımını belirt.

Kod:
{TruncateContent(codeSnippet, 8000)}

Bu şema ile JSON döndür:
{{
    ""suggestions"": [
        {{
            ""type"": ""security"",
            ""title"": ""Güvenlik sorunu başlığı"",
            ""description"": ""Detaylı açıklama ve düzeltme"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""security"",
            ""impactScore"": ""1-10"",
            ""tags"": [""güvenlik"", ""eşzamanlılık"", ""async""]
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
            _logger.LogWarning(ex, "GPT yanıtı ayrıştırılamadı, yedek kullanılıyor");
            
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

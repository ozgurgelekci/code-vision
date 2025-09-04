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
            _logger.LogWarning("OpenAI API key yapılandırılmamış");
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
            return $"PR Başlığı: {prTitle}\nÖzet: OpenAI entegrasyonu henüz yapılandırılmamış.";
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

            return ParseGptSuggestions(content, fileName, "refactor");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT refactör önerileri oluşturma hatası: {FileName}", fileName);
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

            return ParseGptSuggestions(content, "mixed", "potential_issue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT potansiyel sorunlar analizi hatası");
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

            return ParseGptSuggestions(content, "security", "security");
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
        return $@"Aşağıdaki kod değişikliklerini analiz et. Öncelikle 2-3 cümlede ne değiştiğini özetle. 
Sonra ""potansiyel hatalar"", ""geliştirme/refaktör önerileri"" bölümlerini kısa şekilde açıkla.

PR Başlığı: {prTitle}

Kod Değişiklikleri:
{TruncateContent(diffContent, 4000)}

Lütfen yanıtını Türkçe olarak ver ve sonucu şu formatta organize et:
## Özet
[2-3 cümlelik özet]

## Potansiyel Hatalar
[Varsa potansiyel hataları listele]

## Geliştirme Önerileri
[Kod kalitesi ve refaktör önerileri]";
    }

    private string CreateRefactorPrompt(string codeSnippet, string fileName)
    {
        return $@"Bu metot veya sınıfın kodunu daha okunabilir, test edilebilir ve performanslı hale getirecek 
detaylı adımlar ve değişiklikleri sırala. Her öneri için kısa bir kod örneği ver. 
Ayrıca olası riskleri (geri uyumluluk, behavior change) belirt.

Dosya: {fileName}
Kod:
{TruncateContent(codeSnippet, 2000)}

Yanıtını JSON formatında ver:
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
            ""tags"": [""tag1"", ""tag2""]
        }}
    ]
}}";
    }

    private string CreateIssueAnalysisPrompt(string diffContent)
    {
        return $@"Bu kod parçasında potansiyel hataları, performans sorunlarını ve kod kalitesi 
problemlerini tespit et. Her bulgu için öncelik seviyesi ve çözüm önerisi ver.

Kod:
{TruncateContent(diffContent, 3000)}

JSON formatında yanıt ver:
{{
    ""suggestions"": [
        {{
            ""type"": ""potential_issue"",
            ""title"": ""Sorun başlığı"",
            ""description"": ""Detaylı açıklama"",
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
        return $@"Bu kod parçasında güvenlik açıklarını, concurrency sorunlarını, async/await yanlış kullanımlarını 
ve exception handling problemlerini tespit et. Olası yarış durumları, deadlock riskleri 
veya yanlış exception kullanımı var mı kontrol et.

Kod:
{TruncateContent(codeSnippet, 2000)}

JSON formatında yanıt ver:
{{
    ""suggestions"": [
        {{
            ""type"": ""security"",
            ""title"": ""Güvenlik sorunu başlığı"",
            ""description"": ""Detaylı açıklama ve çözüm"",
            ""priority"": ""Low|Medium|High|Critical"",
            ""category"": ""security"",
            ""impactScore"": ""1-10"",
            ""tags"": [""security"", ""concurrency"", ""async""]
        }}
    ]
}}";
    }

    private List<GptSuggestion> ParseGptSuggestions(string? content, string defaultFileName, string defaultType)
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
                        Type = s.Type ?? defaultType,
                        Title = s.Title ?? "GPT Önerisi",
                        Description = s.Description ?? "Açıklama bulunamadı",
                        FilePath = defaultFileName,
                        Priority = ParsePriority(s.Priority),
                        Category = s.Category ?? "code_quality",
                        ImpactScore = s.ImpactScore ?? 5,
                        Tags = s.Tags ?? new List<string>(),
                        SuggestedCode = s.SuggestedCode
                    }));
                }
            }
            else
            {
                // JSON formatı bulunamadı, metin olarak bir öneri oluştur
                suggestions.Add(new GptSuggestion
                {
                    Type = defaultType,
                    Title = "GPT Analizi",
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
            _logger.LogWarning(ex, "GPT yanıtı JSON parse edilemedi, fallback kullanılıyor");
            
            suggestions.Add(new GptSuggestion
            {
                Type = defaultType,
                Title = "GPT Analizi (Parse Hatası)",
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

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;
            
        return content[..maxLength] + "\n... (içerik kısaltıldı)";
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
        public int? ImpactScore { get; set; }
        public List<string>? Tags { get; set; }
    }
}

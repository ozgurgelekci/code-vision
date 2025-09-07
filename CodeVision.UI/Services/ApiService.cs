using System.Text.Json;
using System.Net.Http.Headers;

namespace CodeVision.UI.Services;

public interface IApiService
{
    Task<DashboardData?> GetDashboardDataAsync();
    Task<AnalysisDetail?> GetAnalysisDetailAsync(string analysisId);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://code-vision.up.railway.app";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };
        _httpClient.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
    }

    public async Task<DashboardData?> GetDashboardDataAsync()
    {
        try
        {
            var dashboardUrl = $"/api/dashboard?_={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var response = await _httpClient.GetAsync(dashboardUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔥 RAW API RESPONSE: {content}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                
                var result = JsonSerializer.Deserialize<DashboardData>(content, options);
                Console.WriteLine($"🎯 DESERIALIZED DATA: TotalAnalyses={result?.Overview?.TotalAnalyses}, RecentCount={result?.RecentAnalyses?.Count}");
                
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard verisi alınırken hata oluştu: {Error}", ex.Message);
        }
        
        // Fallback data
        return new DashboardData
        {
            Overview = new OverviewData
            {
                TotalAnalyses = 0,
                AverageQualityScore = 0,
                PendingAnalyses = 0
            },
            RiskDistribution = new RiskDistribution { High = 0, Medium = 0, Low = 0 },
            RecentAnalyses = new List<AnalysisItem>(),
            SystemStatus = new SystemStatus { QueueLength = 0, IsHealthy = true, LastUpdate = DateTime.UtcNow }
        };
    }

    public async Task<AnalysisDetail?> GetAnalysisDetailAsync(string analysisId)
    {
        Console.WriteLine($"🔍 DETAILS API ÇAĞRISI BAŞLATILIYOR: {analysisId}");
        try
        {
            var url = $"/api/analyses/{analysisId}?_={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Console.WriteLine($"🌐 REQUEST URL: {url}");
            var response = await _httpClient.GetAsync(url);
            Console.WriteLine($"📡 RESPONSE STATUS: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔥 DETAILS RESPONSE: {content}");
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<AnalysisDetail>(content, options) ?? new AnalysisDetail();

                // Fallback: gptSuggestions alanını manuel parse et (deser. uyuşmazlıklarına karşı)
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("gptSuggestions", out var gs) && gs.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<GptSuggestion>();
                        foreach (var item in gs.EnumerateArray())
                        {
                            var s = new GptSuggestion
                            {
                                Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                                Title = item.TryGetProperty("title", out var ti) ? ti.GetString() ?? string.Empty : string.Empty,
                                Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                                Priority = item.TryGetProperty("priority", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                                Category = item.TryGetProperty("category", out var c) ? c.GetString() ?? string.Empty : string.Empty,
                                SuggestedCode = item.TryGetProperty("suggestedCode", out var sc) ? sc.GetString() ?? string.Empty : string.Empty,
                                ImpactScore = item.TryGetProperty("impactScore", out var iscore) && iscore.TryGetInt32(out var iv) ? iv : 0,
                                Tags = item.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array
                                    ? tags.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                                    : new List<string>()
                            };
                            list.Add(s);
                        }
                        if (list.Count > 0)
                        {
                            result.GptSuggestions = list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ GPT suggestions manual parse skipped: {ex.Message}");
                }

                Console.WriteLine($"✅ DETAILS DESERIALIZE BAŞARILI: {result?.Id}, GPT Count: {result?.GptSuggestions?.Count}");
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ DETAILS API HATASI: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 DETAILS EXCEPTION: {ex.Message}");
            _logger.LogError(ex, "Analiz detayı alınırken hata oluştu: {AnalysisId} - {Error}", analysisId, ex.Message);
        }
        
        return null;
    }

}

public class DashboardData
{
    public OverviewData? Overview { get; set; }
    public RiskDistribution? RiskDistribution { get; set; }
    public QualityDistribution? QualityDistribution { get; set; }
    public List<AnalysisItem>? RecentAnalyses { get; set; }
    public SystemStatus? SystemStatus { get; set; }
}

public class OverviewData
{
    public int TotalAnalyses { get; set; }
    public int AverageQualityScore { get; set; }
    public int HighQualityCount { get; set; }
    public int MediumQualityCount { get; set; }
    public int LowQualityCount { get; set; }
    public int PendingAnalyses { get; set; }
}

public class RiskDistribution
{
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}

public class QualityDistribution
{
    public int Excellent { get; set; }
    public int Good { get; set; }
    public int NeedsImprovement { get; set; }
}

public class SystemStatus
{
    public int QueueLength { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class AnalysisItem
{
    public string Id { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string PrTitle { get; set; } = string.Empty;
    public string PrAuthor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int QualityScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class AnalysisDetail
{
    public string Id { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string PrTitle { get; set; } = string.Empty;
    public string PrAuthor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int QualityScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<RoslynFinding> RoslynFindings { get; set; } = new();
    public List<GptSuggestion> GptSuggestions { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoslynFinding
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class GptSuggestion
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SuggestedCode { get; set; } = string.Empty;
    public int ImpactScore { get; set; }
    public List<string> Tags { get; set; } = new();
}

using System.Text.Json;

namespace CodeVision.UI.Services;

public interface IApiService
{
    Task<DashboardData?> GetDashboardDataAsync();
    Task<List<AnalysisItem>?> GetRecentAnalysesAsync();
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
        
        var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://codevision-api";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<DashboardData?> GetDashboardDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dashboard");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DashboardData>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard verisi alınırken hata oluştu");
        }
        
        // Fallback data
        return new DashboardData
        {
            TotalAnalyses = 0,
            AverageQuality = 0,
            PendingAnalyses = 0,
            HighRiskCount = 0
        };
    }

    public async Task<List<AnalysisItem>?> GetRecentAnalysesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dashboard/analyses");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<AnalysisItem>>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son analizler alınırken hata oluştu");
        }

        return new List<AnalysisItem>();
    }
}

public class DashboardData
{
    public int TotalAnalyses { get; set; }
    public double AverageQuality { get; set; }
    public int PendingAnalyses { get; set; }
    public int HighRiskCount { get; set; }
}

public class AnalysisItem
{
    public string RepoName { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public double QualityScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

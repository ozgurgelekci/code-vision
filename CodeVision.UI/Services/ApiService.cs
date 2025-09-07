using System.Text.Json;

namespace CodeVision.UI.Services;

public interface IApiService
{
    Task<DashboardData?> GetDashboardDataAsync();
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
    }

    public async Task<DashboardData?> GetDashboardDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dashboard");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<DashboardData>(content, options);
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

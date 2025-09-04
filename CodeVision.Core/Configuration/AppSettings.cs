namespace CodeVision.Core.Configuration;

public class AppSettings
{
    public OpenAISettings OpenAI { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
    public FeatureFlags Features { get; set; } = new();
    public RateLimitingSettings RateLimiting { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.3;
}

public class GitHubSettings
{
    public string WebhookSecret { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.github.com";
}

public class FeatureFlags
{
    public bool EnableGptAnalysis { get; set; } = true;
    public bool EnableRealTimeNotifications { get; set; } = true;
    public bool EnableBackgroundProcessing { get; set; } = true;
    public int MaxConcurrentAnalyses { get; set; } = 5;
}

public class RateLimitingSettings
{
    public RateLimitConfig OpenAI { get; set; } = new();
    public RateLimitConfig GitHub { get; set; } = new();
}

public class RateLimitConfig
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
}

public class SecuritySettings
{
    public bool RequireHttps { get; set; } = false;
    public bool UseSecureHeaders { get; set; } = false;
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

using System.Text.Json.Serialization;

namespace CodeVision.API.Models;

public class GitHubWebhookPayload
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("pull_request")]
    public PullRequest PullRequest { get; set; } = new();
    
    [JsonPropertyName("repository")]
    public Repository Repository { get; set; } = new();
}

public class PullRequest
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public User User { get; set; } = new();
    
    [JsonPropertyName("head")]
    public GitReference Head { get; set; } = new();
    
    [JsonPropertyName("base")]
    public GitReference Base { get; set; } = new();
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class User
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}

public class GitReference
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;
}

public class Repository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("owner")]
    public User Owner { get; set; } = new();
}

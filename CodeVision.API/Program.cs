using Microsoft.EntityFrameworkCore;
using CodeVision.Infrastructure.Data;
using CodeVision.Core.Interfaces;
using CodeVision.Infrastructure.Repositories;
using CodeVision.Infrastructure.Services;
using CodeVision.Infrastructure.Hubs;
using CodeVision.Core.Configuration;
using Microsoft.AspNetCore.HostFiltering;

var builder = WebApplication.CreateBuilder(args);

// Railway host validation fix - disable for production
if (builder.Environment.IsProduction())
{
    builder.WebHost.UseKestrel(options =>
    {
        options.AllowSynchronousIO = true;
    });
    
    // Accept any host for Railway deployment
    builder.Services.Configure<HostFilteringOptions>(options =>
    {
        options.AllowedHosts.Clear();
        options.AllowEmptyHosts = true;
    });
}

// Development configuration
if (builder.Environment.IsDevelopment())
{
    builder.Configuration["AllowedHosts"] = "*";
}

// Configuration
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<GitHubSettings>(builder.Configuration.GetSection("GitHub"));

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - Railway PostgreSQL connection handling
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(connectionString) && 
    (connectionString.Contains("postgresql://") || connectionString.Contains("postgres://")))
{
    try
    {
        // Railway PostgreSQL - convert DATABASE_URL to Npgsql format
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var npgsqlConnectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        
        builder.Services.AddDbContext<CodeVisionDbContext>(options =>
            options.UseNpgsql(npgsqlConnectionString));
            
        Console.WriteLine($"🐘 PostgreSQL connected: Host={uri.Host}, DB={uri.LocalPath.TrimStart('/')}, User={userInfo[0]}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ PostgreSQL connection parsing failed: {ex.Message}");
        Console.WriteLine($"🔗 DATABASE_URL: {connectionString}");
        
        // Fallback to SQLite on connection string error
        var fallbackConnection = "Data Source=codevision.db";
        builder.Services.AddDbContext<CodeVisionDbContext>(options =>
            options.UseSqlite(fallbackConnection));
        Console.WriteLine($"💾 Fallback to SQLite: {fallbackConnection}");
    }
}
else
{
    // Development - SQLite fallback
    var fallbackConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=codevision.db";
    builder.Services.AddDbContext<CodeVisionDbContext>(options =>
        options.UseSqlite(fallbackConnection));
        
    Console.WriteLine($"💾 Using SQLite: {fallbackConnection}");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("⚠️  DATABASE_URL not found, using SQLite for development");
    }
}

// Repository & Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPullRequestAnalysisService, PullRequestAnalysisService>();
builder.Services.AddScoped<IRoslynAnalyzerService, RoslynAnalyzerService>();
builder.Services.AddScoped<IGptAnalysisService, GptAnalysisService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background Services
builder.Services.AddSingleton<IAnalysisQueue, InMemoryAnalysisQueue>();
builder.Services.AddHostedService<AnalysisBackgroundService>();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins("http://localhost:3001", "https://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Railway dynamic port configuration
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Automatic database migration for Railway deployment  
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CodeVisionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🔄 Starting database migration...");
        
        // Ensure database exists and apply migrations
        logger.LogInformation("🔍 Checking database and migrations...");
        
        // Create database if it doesn't exist  
        await dbContext.Database.EnsureCreatedAsync();
        
        // Apply all pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("⏳ Applying {Count} pending migrations: {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
            
            await dbContext.Database.MigrateAsync();
            
            logger.LogInformation("✅ Database migration completed successfully!");
        }
        else
        {
            logger.LogInformation("✅ Database is up to date, no migrations needed.");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Database migration failed: {Error}", ex.Message);
        
        // Continue startup even if migration fails (for Railway cold starts)
        logger.LogWarning("⚠️  App starting without database migration...");
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production için de Swagger göster (Railway'de API test için)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeVision API v1");
        c.RoutePrefix = "swagger"; // /swagger URL'de açılır
    });
}

app.UseCors("AllowUI");
app.UseAuthorization();
app.MapControllers();
app.MapHub<AnalysisNotificationHub>("/hubs/analysis");

app.Run();
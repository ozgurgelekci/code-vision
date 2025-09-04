using Microsoft.EntityFrameworkCore;
using CodeVision.Infrastructure.Data;
using CodeVision.Core.Interfaces;
using CodeVision.Infrastructure.Repositories;
using CodeVision.Infrastructure.Services;
using CodeVision.Infrastructure.Hubs;
using CodeVision.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

// Database - Railway uses DATABASE_URL environment variable
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString?.Contains("postgresql://") == true || connectionString?.Contains("postgres://") == true)
{
    // PostgreSQL for production (Railway)
    builder.Services.AddDbContext<CodeVisionDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // SQLite for development
    builder.Services.AddDbContext<CodeVisionDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=codevision.db"));
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

// Automatic database migration for Railway deployment  
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CodeVisionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🔄 Starting database migration...");
        
        // Check if database exists and apply migrations
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("⏳ Applying {Count} pending migrations...", 
                dbContext.Database.GetPendingMigrations().Count());
            
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
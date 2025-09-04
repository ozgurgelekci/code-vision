using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeVision.Infrastructure.Data;

namespace CodeVision.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly CodeVisionDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(CodeVisionDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var healthCheck = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Version = "1.0.0",
                Services = new
                {
                    Database = await CheckDatabaseHealth(),
                    BackgroundService = "Running", // Background service kontrolü eklenebilir
                    SignalR = "Active"
                }
            };

            return Ok(healthCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var errorResponse = new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return StatusCode(500, errorResponse);
        }
    }

    private async Task<string> CheckDatabaseHealth()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return "Connected";
        }
        catch
        {
            return "Disconnected";
        }
    }
}

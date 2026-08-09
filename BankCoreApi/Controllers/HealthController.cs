using BankCoreApi.Data;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("healthz")]
public class HealthController : ControllerBase
{
    private readonly BankDbContext _dbContext;

    public HealthController(BankDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Health Check Probe Endpoint (Pertemuan 9 - Docker & Kubernetes Hardening)
    /// Memeriksa status kesehatan aplikasi dan koneksi basis data.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        bool canConnect = await _dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Database = "Connected",
                App = "BankCoreApi v1.0"
            });
        }

        return StatusCode(530, new
        {
            Status = "Unhealthy",
            Timestamp = DateTime.UtcNow,
            Database = "Disconnected"
        });
    }
}

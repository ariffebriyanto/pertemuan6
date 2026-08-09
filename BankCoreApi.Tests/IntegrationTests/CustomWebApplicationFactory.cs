using BankCoreApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankCoreApi.Tests.IntegrationTests;

/// <summary>
/// WebApplicationFactory Kustom untuk Automated Integration Testing (Pertemuan 7)
/// Mengganti DbContext dengan InMemory Database yang terisolasi untuk pengujian HTTP Endpoints.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Hapus registrasi DbContext bawaan jika ada
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<BankDbContext>) ||
                     d.ServiceType == typeof(BankDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Tambahkan InMemory Database khusus untuk Integration Tests
            services.AddDbContext<BankDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestBankDb_{Guid.NewGuid()}");
            });
        });

        builder.UseEnvironment("Testing");
    }
}

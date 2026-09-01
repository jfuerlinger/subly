using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subly.Infrastructure.Persistence;

namespace Subly.Api.Tests;

/// <summary>
/// Hosts the API with ASPNETCORE_ENVIRONMENT=Production, to verify that environment-gated
/// endpoints (e.g. AdminController) actually refuse to run outside Development/Testing.
/// Program.cs reads Authentication:Jwt:SigningKey/ConnectionStrings:DefaultConnection directly off
/// builder.Configuration before WebApplicationFactory's ConfigureAppConfiguration hook takes effect,
/// so these have to be supplied as real process environment variables instead.
/// </summary>
public sealed class ProductionWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"subly-prod-guard-tests-{Guid.NewGuid():N}";

    public ProductionWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Authentication__Jwt__SigningKey", "production-test-only-jwt-signing-key-not-a-real-secret");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=unused;Username=unused;Password=unused");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            var dbName = _dbName;
            services.Replace(ServiceDescriptor.Scoped<SublyDbContext>(_ =>
            {
                var opts = new DbContextOptionsBuilder<SublyDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;
                return new SublyDbContext(opts);
            }));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("Authentication__Jwt__SigningKey", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
    }
}

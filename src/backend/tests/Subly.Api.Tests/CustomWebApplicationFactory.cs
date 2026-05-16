using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subly.Infrastructure.Persistence;

namespace Subly.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"subly-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var dbName = _dbName;
            services.Replace(ServiceDescriptor.Scoped<SublyDbContext>(_ =>
            {
                var opts = new DbContextOptionsBuilder<SublyDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
                return new SublyDbContext(opts);
            }));
        });
    }
}

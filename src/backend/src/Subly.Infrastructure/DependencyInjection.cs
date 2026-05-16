using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Abstractions;
using Subly.Infrastructure.Persistence;
using Subly.Infrastructure.Persistence.Repositories;
using Subly.Infrastructure.Seeding;

namespace Subly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=subly.db";

        services.AddDbContext<SublyDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
        services.AddSingleton<IDateProvider, SystemDateProvider>();

        return services;
    }

    public static async Task EnsureDatabaseInitializedAsync(this IServiceProvider services, bool seed = false, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (seed)
        {
            await SublyDataSeeder.SeedAsync(dbContext, cancellationToken);
        }
    }
}

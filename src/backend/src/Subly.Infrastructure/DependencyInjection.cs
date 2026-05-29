using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Abstractions;
using Subly.Application.Services;
using Subly.Infrastructure.Persistence;
using Subly.Infrastructure.Persistence.Repositories;
using Subly.Infrastructure.Security;
using Subly.Infrastructure.Seeding;
using Subly.Infrastructure.Services;

namespace Subly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("sublydb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No database connection string configured (expected 'sublydb' or 'DefaultConnection').");

        services.AddDbContext<SublyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IDateProvider, SystemDateProvider>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }

    public static async Task EnsureDatabaseInitializedAsync(this IServiceProvider services, bool seed = false, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (seed)
        {
            await SublyDataSeeder.SeedAsync(dbContext, passwordHasher, cancellationToken);
        }
    }

    public static async Task ResetDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SublyDataSeeder.ForceSeedAsync(dbContext, passwordHasher, cancellationToken);
    }
}

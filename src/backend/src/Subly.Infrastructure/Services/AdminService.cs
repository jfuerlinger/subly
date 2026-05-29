using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Infrastructure.Persistence;
using Subly.Infrastructure.Seeding;

namespace Subly.Infrastructure.Services;

public sealed class AdminService(ISubscriptionRepository subscriptionRepository, SublyDbContext dbContext, IPasswordHasher passwordHasher) : IAdminService
{
    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await subscriptionRepository.DeleteAllAsync(cancellationToken);
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await subscriptionRepository.DeleteAllAsync(cancellationToken);
        await SublyDataSeeder.ForceSeedAsync(dbContext, passwordHasher, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<DatabaseResetResultDto> ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var steps = new List<string>(capacity: 3);

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        steps.Add("Vorhandene Datenbank gelöscht");

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        steps.Add("Migrationen erneut angewendet");

        await SublyDataSeeder.ForceSeedAsync(dbContext, passwordHasher, cancellationToken);
        steps.Add("Seed-Daten neu eingespielt");

        return new DatabaseResetResultDto(steps, DateTimeOffset.UtcNow);
    }
}

using Subly.Application.Abstractions;
using Subly.Application.Services;
using Subly.Infrastructure.Persistence;
using Subly.Infrastructure.Seeding;

namespace Subly.Infrastructure.Services;

public sealed class AdminService(ISubscriptionRepository subscriptionRepository, SublyDbContext dbContext) : IAdminService
{
    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await subscriptionRepository.DeleteAllAsync(cancellationToken);
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await subscriptionRepository.DeleteAllAsync(cancellationToken);
            await SublyDataSeeder.ForceSeedAsync(dbContext, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Repositories;

public sealed class EfSubscriptionRepository(SublyDbContext dbContext) : ISubscriptionRepository
{
    public async Task<IReadOnlyList<Subscription>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Subscriptions.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Subscriptions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Subscriptions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.Subscriptions.Remove(entity);
        return true;
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Subscriptions.ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

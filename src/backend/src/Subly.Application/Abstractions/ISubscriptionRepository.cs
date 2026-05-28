using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface ISubscriptionRepository
{
    Task<IReadOnlyList<Subscription>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

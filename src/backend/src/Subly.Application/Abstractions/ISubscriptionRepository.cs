using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface ISubscriptionRepository
{
    Task<IReadOnlyList<Subscription>> ListAsync(CancellationToken cancellationToken = default);

    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

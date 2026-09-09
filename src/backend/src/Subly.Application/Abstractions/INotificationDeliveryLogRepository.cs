using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface INotificationDeliveryLogRepository
{
    Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

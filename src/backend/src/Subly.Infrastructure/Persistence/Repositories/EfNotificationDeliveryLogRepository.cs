using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Repositories;

internal sealed class EfNotificationDeliveryLogRepository(SublyDbContext dbContext) : INotificationDeliveryLogRepository
{
    public Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationDeliveryLogs.AnyAsync(
            x => x.SubscriptionId == subscriptionId && x.Channel == channel && x.ForPaymentDate == forPaymentDate,
            cancellationToken);
    }

    public async Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.NotificationDeliveryLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

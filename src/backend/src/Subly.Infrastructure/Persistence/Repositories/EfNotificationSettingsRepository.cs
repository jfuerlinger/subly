using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Repositories;

internal sealed class EfNotificationSettingsRepository(SublyDbContext dbContext) : INotificationSettingsRepository
{
    public Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationSettings.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
    {
        await dbContext.NotificationSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface INotificationSettingsRepository
{
    Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

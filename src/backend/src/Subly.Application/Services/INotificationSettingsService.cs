using Subly.Application.Contracts;

namespace Subly.Application.Services;

public interface INotificationSettingsService
{
    Task<NotificationSettingsDto> GetForCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<NotificationSettingsDto> UpdateAsync(UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default);
}

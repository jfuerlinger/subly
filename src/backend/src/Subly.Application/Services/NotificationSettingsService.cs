using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class NotificationSettingsService(
    INotificationSettingsRepository repository,
    ICurrentUserProvider currentUserProvider) : INotificationSettingsService
{
    private const int DefaultLeadDays = 3;

    public async Task<NotificationSettingsDto> GetForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var settings = await repository.GetByUserIdAsync(userId, cancellationToken);

        return settings is null
            ? new NotificationSettingsDto(DefaultLeadDays, false)
            : ToDto(settings);
    }

    public async Task<NotificationSettingsDto> UpdateAsync(UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var channels = request.EmailEnabled ? NotificationChannel.Email : NotificationChannel.None;
        var settings = await repository.GetByUserIdAsync(userId, cancellationToken);

        if (settings is null)
        {
            settings = NotificationSettings.Create(userId, request.LeadDays, channels);
            await repository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.Update(request.LeadDays, channels);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private static NotificationSettingsDto ToDto(NotificationSettings settings)
    {
        return new NotificationSettingsDto(settings.LeadDays, settings.Channels.HasFlag(NotificationChannel.Email));
    }
}

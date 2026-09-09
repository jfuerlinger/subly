using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class PaymentReminderService(
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    INotificationSettingsRepository notificationSettingsRepository,
    INotificationDeliveryLogRepository deliveryLogRepository,
    IEmailSender emailSender,
    IDateProvider dateProvider) : IPaymentReminderService
{
    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await subscriptionRepository.ListAllActiveAsync(cancellationToken);
        var today = dateProvider.Today;

        foreach (var group in subscriptions.GroupBy(x => x.UserId))
        {
            var settings = await notificationSettingsRepository.GetByUserIdAsync(group.Key, cancellationToken);
            if (settings is null || !settings.Channels.HasFlag(NotificationChannel.Email))
            {
                continue;
            }

            foreach (var subscription in group)
            {
                var dueDate = subscription.NextPaymentDate.AddDays(-settings.LeadDays);
                if (dueDate != today)
                {
                    continue;
                }

                var alreadySent = await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate, cancellationToken);
                if (alreadySent)
                {
                    continue;
                }

                var user = await userRepository.GetByIdAsync(group.Key, cancellationToken);
                if (user is null)
                {
                    continue;
                }

                await emailSender.SendAsync(
                    user.Email,
                    $"Erinnerung: {subscription.Name} wird bald abgebucht",
                    BuildReminderHtml(subscription, settings.LeadDays),
                    cancellationToken);

                var log = NotificationDeliveryLog.Create(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate);
                await deliveryLogRepository.AddAsync(log, cancellationToken);
                await deliveryLogRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static string BuildReminderHtml(Subscription subscription, int leadDays)
    {
        return $"""
            <p>Hallo,</p>
            <p>dein Abo <strong>{subscription.Name}</strong> ({subscription.Vendor}) wird in {leadDays} Tag(en), am {subscription.NextPaymentDate:dd.MM.yyyy}, mit {subscription.Price:0.00} € abgebucht.</p>
            """;
    }
}

namespace Subly.Domain.Models;

public sealed class NotificationDeliveryLog
{
    private NotificationDeliveryLog() { }

    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DateOnly ForPaymentDate { get; private set; }
    public DateTimeOffset SentAtUtc { get; private set; }

    public static NotificationDeliveryLog Create(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        }

        if (channel is NotificationChannel.None)
        {
            throw new ArgumentException("A concrete channel is required.", nameof(channel));
        }

        return new NotificationDeliveryLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Channel = channel,
            ForPaymentDate = forPaymentDate,
            SentAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

namespace Subly.Domain.Models;

public sealed class NotificationSettings
{
    private NotificationSettings() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int LeadDays { get; private set; }
    public NotificationChannel Channels { get; private set; }

    public static NotificationSettings Create(Guid userId, int leadDays, NotificationChannel channels)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ValidateLeadDays(leadDays);

        return new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeadDays = leadDays,
            Channels = channels,
        };
    }

    public void Update(int leadDays, NotificationChannel channels)
    {
        ValidateLeadDays(leadDays);
        LeadDays = leadDays;
        Channels = channels;
    }

    private static void ValidateLeadDays(int leadDays)
    {
        if (leadDays is < 0 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(leadDays), "Lead days must be between 0 and 90.");
        }
    }
}

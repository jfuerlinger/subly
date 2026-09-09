namespace Subly.Domain.Models;

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Signal = 2,
}

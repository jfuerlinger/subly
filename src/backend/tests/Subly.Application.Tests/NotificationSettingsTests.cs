using FluentAssertions;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class NotificationSettingsTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_ShouldSucceed_WithValidLeadDays()
    {
        var settings = NotificationSettings.Create(UserId, 3, NotificationChannel.Email);

        settings.UserId.Should().Be(UserId);
        settings.LeadDays.Should().Be(3);
        settings.Channels.Should().Be(NotificationChannel.Email);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public void Create_ShouldThrow_WhenLeadDaysOutOfRange(int leadDays)
    {
        var act = () => NotificationSettings.Create(UserId, leadDays, NotificationChannel.Email);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        var act = () => NotificationSettings.Create(Guid.Empty, 3, NotificationChannel.Email);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldChangeLeadDaysAndChannels()
    {
        var settings = NotificationSettings.Create(UserId, 3, NotificationChannel.None);

        settings.Update(7, NotificationChannel.Email);

        settings.LeadDays.Should().Be(7);
        settings.Channels.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public void NotificationDeliveryLog_Create_ShouldSetFields()
    {
        var subscriptionId = Guid.NewGuid();
        var forPaymentDate = new DateOnly(2026, 9, 15);

        var log = NotificationDeliveryLog.Create(subscriptionId, NotificationChannel.Email, forPaymentDate);

        log.SubscriptionId.Should().Be(subscriptionId);
        log.Channel.Should().Be(NotificationChannel.Email);
        log.ForPaymentDate.Should().Be(forPaymentDate);
    }

    [Fact]
    public void NotificationDeliveryLog_Create_ShouldThrow_WhenChannelIsNone()
    {
        var act = () => NotificationDeliveryLog.Create(Guid.NewGuid(), NotificationChannel.None, new DateOnly(2026, 9, 15));

        act.Should().Throw<ArgumentException>();
    }
}

using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class NotificationSettingsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task GetForCurrentUserAsync_ShouldReturnDefaults_WhenNoSettingsExist()
    {
        var service = new NotificationSettingsService(
            new InMemoryNotificationSettingsRepository(),
            new FixedCurrentUserProvider(CurrentUserId));

        var result = await service.GetForCurrentUserAsync();

        result.LeadDays.Should().Be(3);
        result.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldCreateSettings_WhenNoneExistYet()
    {
        var repository = new InMemoryNotificationSettingsRepository();
        var service = new NotificationSettingsService(repository, new FixedCurrentUserProvider(CurrentUserId));

        var result = await service.UpdateAsync(new UpdateNotificationSettingsRequest(5, true));

        result.LeadDays.Should().Be(5);
        result.EmailEnabled.Should().BeTrue();
        var stored = await repository.GetByUserIdAsync(CurrentUserId);
        stored.Should().NotBeNull();
        stored!.Channels.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSettings()
    {
        var repository = new InMemoryNotificationSettingsRepository();
        var service = new NotificationSettingsService(repository, new FixedCurrentUserProvider(CurrentUserId));
        await service.UpdateAsync(new UpdateNotificationSettingsRequest(5, true));

        var result = await service.UpdateAsync(new UpdateNotificationSettingsRequest(10, false));

        result.LeadDays.Should().Be(10);
        result.EmailEnabled.Should().BeFalse();
        var stored = await repository.GetByUserIdAsync(CurrentUserId);
        stored!.LeadDays.Should().Be(10);
        stored.Channels.Should().Be(NotificationChannel.None);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenLeadDaysOutOfRange()
    {
        var service = new NotificationSettingsService(
            new InMemoryNotificationSettingsRepository(),
            new FixedCurrentUserProvider(CurrentUserId));

        var act = () => service.UpdateAsync(new UpdateNotificationSettingsRequest(-1, true));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private sealed class FixedCurrentUserProvider(Guid userId) : ICurrentUserProvider
    {
        public Guid GetRequiredUserId() => userId;
    }

    private sealed class InMemoryNotificationSettingsRepository : INotificationSettingsRepository
    {
        private readonly List<NotificationSettings> _items = [];

        public Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.UserId == userId));
        }

        public Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
        {
            _items.Add(settings);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

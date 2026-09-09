using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class PaymentReminderServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSendEmail_WhenDueDateMatchesToday()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var subscriptionRepository = new InMemorySubscriptionRepository([subscription]);
        var userRepository = new InMemoryUserRepository([user]);
        var settingsRepository = new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]);
        var deliveryLogRepository = new InMemoryNotificationDeliveryLogRepository();
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            subscriptionRepository, userRepository, settingsRepository, deliveryLogRepository, emailSender, new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
        emailSender.SentMessages[0].ToEmail.Should().Be("user@example.com");
        (await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldPropagateAndNotLogDelivery_WhenSendAsyncThrows()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var deliveryLogRepository = new InMemoryNotificationDeliveryLogRepository();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]),
            deliveryLogRepository,
            new ThrowingEmailSender(),
            new FixedDateProvider(today));

        var act = async () => await service.ProcessDueRemindersAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate)).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenDueDateDoesNotMatchToday()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(10), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenEmailChannelDisabled()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.None)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenNoSettingsExistForUser()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository(),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldNotSendTwice_ForSamePaymentDate()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();
        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldOnlyNotifyMatchingUser_AcrossMultipleUsers()
    {
        var today = new DateOnly(2026, 9, 8);
        var dueUser = CreateUser("due@example.com");
        var otherUser = CreateUser("other@example.com");
        var dueSubscription = Subscription.Create(dueUser.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var otherSubscription = Subscription.Create(otherUser.Id, "Spotify", "Spotify", CategoryId, 9.99m, BillingCycle.Monthly, today.AddDays(20), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([dueSubscription, otherSubscription]),
            new InMemoryUserRepository([dueUser, otherUser]),
            new InMemoryNotificationSettingsRepository([
                NotificationSettings.Create(dueUser.Id, 3, NotificationChannel.Email),
                NotificationSettings.Create(otherUser.Id, 3, NotificationChannel.Email),
            ]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
        emailSender.SentMessages[0].ToEmail.Should().Be("due@example.com");
    }

    private static User CreateUser(string email)
    {
        return User.Create("Max", "Muster", email, "hash", "salt", 1);
    }

    private sealed class FixedDateProvider(DateOnly today) : IDateProvider
    {
        public DateOnly Today => today;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string ToEmail, string Subject, string BodyHtml)> SentMessages { get; } = [];

        public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((toEmail, subject, bodyHtml));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("smtp down");
        }
    }

    private sealed class InMemorySubscriptionRepository(IEnumerable<Subscription>? seed = null) : ISubscriptionRepository
    {
        private readonly List<Subscription> _items = seed?.ToList() ?? [];

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            _items.Add(subscription);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.RemoveAll(x => x.Id == id && x.UserId == userId) > 0);
        }

        public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }

        public Task<Subscription?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Id == id && x.UserId == userId));
        }

        public Task<IReadOnlyList<Subscription>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => x.UserId == userId).ToList());
        }

        public Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => x.Status == SubscriptionStatus.Active).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserRepository(IEnumerable<User> seed) : IUserRepository
    {
        private readonly List<User> _items = seed.ToList();

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Email == email));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _items.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationSettingsRepository(IEnumerable<NotificationSettings>? seed = null) : INotificationSettingsRepository
    {
        private readonly List<NotificationSettings> _items = seed?.ToList() ?? [];

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

    private sealed class InMemoryNotificationDeliveryLogRepository : INotificationDeliveryLogRepository
    {
        private readonly List<NotificationDeliveryLog> _items = [];

        public Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.Any(x => x.SubscriptionId == subscriptionId && x.Channel == channel && x.ForPaymentDate == forPaymentDate));
        }

        public Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default)
        {
            _items.Add(log);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

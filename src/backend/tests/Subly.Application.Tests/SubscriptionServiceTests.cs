using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class SubscriptionServiceTests
{
    private static readonly string[] DefaultCategories = ["streaming", "software", "insurance", "telecom", "energy", "fitness", "news", "cloud", "membership"];

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateAggregates()
    {
        var now = new DateOnly(2026, 5, 16);
        var repository = new InMemorySubscriptionRepository(
        [
            Subscription.Create("Netflix", "Netflix", "streaming", 17.99m, BillingCycle.Monthly, now.AddDays(5), "Visa", now.AddYears(-1)),
            Subscription.Create("Prime", "Amazon", "streaming", 89.90m, BillingCycle.Yearly, now.AddDays(14), "PayPal", now.AddYears(-2)),
            Subscription.Create("Paused", "Provider", "software", 10m, BillingCycle.Monthly, now.AddDays(7), "Visa", now.AddMonths(-3), status: SubscriptionStatus.Paused),
        ]);
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(now));

        var summary = await service.GetDashboardSummaryAsync();

        summary.ActiveSubscriptionsCount.Should().Be(2);
        summary.MonthlyTotal.Should().Be(25.48m);
        summary.YearlyTotal.Should().Be(305.78m);
        summary.UpcomingPaymentsTotal30Days.Should().Be(107.89m);
        summary.UpcomingPaymentsCount30Days.Should().Be(2);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ShouldPersistAndReturnCreatedEntity()
    {
        var now = new DateOnly(2026, 5, 16);
        var repository = new InMemorySubscriptionRepository();
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(now));
        var request = new CreateSubscriptionRequest(
            Name: "ChatGPT Plus",
            Vendor: "OpenAI",
            Category: "software",
            Price: 22m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: now.AddDays(3),
            PaymentMethod: "Visa",
            StartedAt: now.AddMonths(-2),
            CancelledAt: null);

        var created = await service.CreateSubscriptionAsync(request);
        var stored = await repository.GetByIdAsync(created.Id);

        created.Name.Should().Be("ChatGPT Plus");
        created.Status.Should().Be(SubscriptionStatus.Active);
        stored.Should().NotBeNull();
        stored!.StartedAt.Should().Be(now.AddMonths(-2));
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenSubscriptionDoesNotExist()
    {
        var repository = new InMemorySubscriptionRepository();
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(new DateOnly(2026, 5, 16)));

        var result = await service.UpdateStatusAsync(Guid.NewGuid(), SubscriptionStatus.Cancelled, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetCancelledDateToToday_WhenNoDateIsProvided()
    {
        var now = new DateOnly(2026, 5, 16);
        var existing = Subscription.Create("Notion", "Notion", "software", 9.5m, BillingCycle.Monthly, now.AddDays(2), "PayPal", now.AddMonths(-10));
        var repository = new InMemorySubscriptionRepository([existing]);
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(now));

        var updated = await service.UpdateStatusAsync(existing.Id, SubscriptionStatus.Cancelled, null);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SubscriptionStatus.Cancelled);
        updated.CancelledAt.Should().Be(now);
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_ShouldRemoveExistingSubscription()
    {
        var existing = Subscription.Create("Notion", "Notion", "software", 9.5m, BillingCycle.Monthly, new DateOnly(2026, 5, 18), "PayPal", new DateOnly(2025, 1, 1));
        var repository = new InMemorySubscriptionRepository([existing]);
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(new DateOnly(2026, 5, 16)));

        var deleted = await service.DeleteSubscriptionAsync(existing.Id);
        var afterDelete = await repository.GetByIdAsync(existing.Id);

        deleted.Should().BeTrue();
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnSubscriptionsOrderedByName()
    {
        var now = new DateOnly(2026, 5, 16);
        var repository = new InMemorySubscriptionRepository(
        [
            Subscription.Create("Zeta", "Vendor", "software", 10m, BillingCycle.Monthly, now.AddDays(1), "Visa", now),
            Subscription.Create("Alpha", "Vendor", "software", 10m, BillingCycle.Monthly, now.AddDays(1), "Visa", now),
        ]);
        var service = new SubscriptionService(repository, new InMemoryCategoryRepository(DefaultCategories), new FixedDateProvider(now));

        var result = await service.GetSubscriptionsAsync();

        result.Select(x => x.Name).Should().ContainInOrder("Alpha", "Zeta");
    }

    private sealed class FixedDateProvider(DateOnly today) : IDateProvider
    {
        public DateOnly Today => today;
    }

    private sealed class InMemorySubscriptionRepository(IEnumerable<Subscription>? seed = null) : ISubscriptionRepository
    {
        private readonly List<Subscription> _items = seed?.ToList() ?? [];

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            _items.Add(subscription);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _items.RemoveAll(x => x.Id == id) > 0;
            return Task.FromResult(removed);
        }

        public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }

        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Subscription>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCategoryRepository(IEnumerable<string>? seedNames = null) : ICategoryRepository
    {
        private readonly List<Category> _items = seedNames?.Select(Category.Create).ToList() ?? [];

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(_items);
        }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(c => c.Id == id));
        }

        public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(c => c.Name == name));
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            _items.Add(category);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

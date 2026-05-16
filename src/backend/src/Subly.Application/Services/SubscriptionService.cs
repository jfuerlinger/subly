using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class SubscriptionService(ISubscriptionRepository repository, IDateProvider dateProvider) : ISubscriptionService
{
    private static readonly HashSet<string> KnownCategories =
    [
        "streaming",
        "software",
        "insurance",
        "telecom",
        "energy",
        "fitness",
        "news",
        "cloud",
        "membership",
    ];

    public async Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await repository.ListAsync(cancellationToken);
        return subscriptions
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto)
            .ToArray();
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetByIdAsync(id, cancellationToken);
        return subscription is null ? null : ToDto(subscription);
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var subscription = Subscription.Create(
            request.Name,
            request.Vendor,
            request.Category.ToLowerInvariant(),
            request.Price,
            request.Cycle,
            request.NextPaymentDate,
            request.PaymentMethod,
            dateProvider.Today);

        await repository.AddAsync(subscription, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    public async Task<SubscriptionDto?> UpdateStatusAsync(Guid id, SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetByIdAsync(id, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        subscription.UpdateStatus(status);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await repository.ListAsync(cancellationToken);
        var activeSubscriptions = subscriptions.Where(x => x.Status is SubscriptionStatus.Active).ToArray();
        var today = dateProvider.Today;
        var end = today.AddDays(30);

        var monthly = activeSubscriptions.Sum(ToMonthlyValue);
        var yearly = activeSubscriptions.Sum(ToYearlyValue);
        var upcoming = activeSubscriptions.Where(x => x.NextPaymentDate >= today && x.NextPaymentDate <= end).ToArray();

        return new DashboardSummaryDto(
            MonthlyTotal: decimal.Round(monthly, 2, MidpointRounding.AwayFromZero),
            YearlyTotal: decimal.Round(yearly, 2, MidpointRounding.AwayFromZero),
            ActiveSubscriptionsCount: activeSubscriptions.Length,
            UpcomingPaymentsTotal30Days: decimal.Round(upcoming.Sum(x => x.Price), 2, MidpointRounding.AwayFromZero),
            UpcomingPaymentsCount30Days: upcoming.Length);
    }

    private static decimal ToMonthlyValue(Subscription subscription)
    {
        return subscription.Cycle is BillingCycle.Yearly ? subscription.Price / 12m : subscription.Price;
    }

    private static decimal ToYearlyValue(Subscription subscription)
    {
        return subscription.Cycle is BillingCycle.Yearly ? subscription.Price : subscription.Price * 12m;
    }

    private static SubscriptionDto ToDto(Subscription subscription)
    {
        return new SubscriptionDto(
            subscription.Id,
            subscription.Name,
            subscription.Vendor,
            subscription.Category,
            subscription.Price,
            subscription.Cycle,
            subscription.NextPaymentDate,
            subscription.PaymentMethod,
            subscription.Status,
            subscription.AutoRenew,
            subscription.StartedAt);
    }

    private static void ValidateRequest(CreateSubscriptionRequest request)
    {
        if (request.Price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Price), "Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request.Category));
        }

        if (!KnownCategories.Contains(request.Category.ToLowerInvariant()))
        {
            throw new ArgumentException($"Unknown category '{request.Category}'.", nameof(request.Category));
        }
    }
}

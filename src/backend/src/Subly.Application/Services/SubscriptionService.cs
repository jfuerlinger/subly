using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class SubscriptionService(
    ISubscriptionRepository repository,
    ICategoryRepository categoryRepository,
    IDateProvider dateProvider) : ISubscriptionService
{

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
        await ValidateRequestAsync(request, cancellationToken);

        var initialStatus = request.CancelledAt.HasValue ? SubscriptionStatus.Cancelled : SubscriptionStatus.Active;
        var subscription = Subscription.Create(
            request.Name,
            request.Vendor,
            request.Category.ToLowerInvariant(),
            request.Price,
            request.Cycle,
            request.NextPaymentDate,
            request.PaymentMethod,
            request.StartedAt,
            request.CancelledAt,
            initialStatus);

        await repository.AddAsync(subscription, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    public async Task<SubscriptionDto?> UpdateStatusAsync(Guid id, SubscriptionStatus status, DateOnly? cancelledAt, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetByIdAsync(id, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        DateOnly? effectiveCancelledAt = status is SubscriptionStatus.Cancelled
            ? cancelledAt ?? dateProvider.Today
            : null;
        subscription.UpdateStatus(status, effectiveCancelledAt);
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
            subscription.StartedAt,
            subscription.CancelledAt);
    }

    private async Task ValidateRequestAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (request.Price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Price), "Price must be greater than zero.");
        }

        if (request.CancelledAt.HasValue && request.CancelledAt.Value < request.StartedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CancelledAt), "Cancelled date cannot be earlier than start date.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request.Category));
        }

        var category = await categoryRepository.GetByNameAsync(request.Category.ToLowerInvariant(), cancellationToken);
        if (category is null)
        {
            throw new ArgumentException($"Unknown category '{request.Category}'.", nameof(request.Category));
        }
    }
}

using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> UpdateStatusAsync(Guid id, SubscriptionStatus status, DateOnly? cancelledAt, CancellationToken cancellationToken = default);

    Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}

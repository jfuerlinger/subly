using Subly.Domain.Models;

namespace Subly.Application.Contracts;

public sealed record SubscriptionDto(
    Guid Id,
    string Name,
    string Vendor,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    BillingCycle Cycle,
    DateOnly NextPaymentDate,
    string PaymentMethod,
    SubscriptionStatus Status,
    bool AutoRenew,
    DateOnly StartedAt,
    DateOnly? CancelledAt,
    string? LogoUrl = null);

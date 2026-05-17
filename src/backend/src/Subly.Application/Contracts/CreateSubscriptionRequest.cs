using Subly.Domain.Models;

namespace Subly.Application.Contracts;

public sealed record CreateSubscriptionRequest(
    string Name,
    string Vendor,
    string Category,
    decimal Price,
    BillingCycle Cycle,
    DateOnly NextPaymentDate,
    string PaymentMethod,
    DateOnly StartedAt,
    DateOnly? CancelledAt);

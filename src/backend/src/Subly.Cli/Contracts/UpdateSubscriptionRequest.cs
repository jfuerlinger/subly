namespace Subly.Cli.Contracts;

public record UpdateSubscriptionRequest(
    string Name,
    string Vendor,
    Guid CategoryId,
    decimal Price,
    string Cycle,
    DateOnly NextPaymentDate,
    string PaymentMethod,
    DateOnly StartedAt,
    DateOnly? CancelledAt);

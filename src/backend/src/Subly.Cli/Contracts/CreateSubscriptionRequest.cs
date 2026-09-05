namespace Subly.Cli.Contracts;

public record CreateSubscriptionRequest(
    string Name,
    string Vendor,
    Guid CategoryId,
    decimal Price,
    string Cycle,
    DateOnly NextPaymentDate,
    string PaymentMethod,
    DateOnly StartedAt,
    DateOnly? CancelledAt,
    string? LogoUrl = null);

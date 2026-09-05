namespace Subly.Cli.Contracts;

public record SubscriptionDto(
    Guid Id,
    string Name,
    string Vendor,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    string Cycle,
    DateOnly NextPaymentDate,
    string PaymentMethod,
    string Status,
    bool AutoRenew,
    DateOnly StartedAt,
    DateOnly? CancelledAt,
    string? LogoUrl = null);

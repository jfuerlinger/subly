namespace Subly.Cli.Contracts;

public record UpdateSubscriptionStatusRequest(string Status, DateOnly? CancelledAt);

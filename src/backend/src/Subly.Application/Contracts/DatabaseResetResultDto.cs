namespace Subly.Application.Contracts;

public sealed record DatabaseResetResultDto(IReadOnlyList<string> Steps, DateTimeOffset CompletedAtUtc);

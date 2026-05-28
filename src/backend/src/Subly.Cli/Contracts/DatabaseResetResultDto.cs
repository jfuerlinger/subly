namespace Subly.Cli.Contracts;

public sealed record DatabaseResetResultDto(IReadOnlyList<string> Steps, DateTimeOffset CompletedAtUtc);

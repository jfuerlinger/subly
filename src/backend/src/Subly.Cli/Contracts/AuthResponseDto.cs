namespace Subly.Cli.Contracts;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    AuthenticatedUserDto User);

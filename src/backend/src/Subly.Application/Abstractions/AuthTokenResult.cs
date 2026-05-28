namespace Subly.Application.Abstractions;

public sealed record AuthTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);

namespace Subly.Cli.Contracts;

public sealed record LoginUserRequest(
    string Email,
    string Password);

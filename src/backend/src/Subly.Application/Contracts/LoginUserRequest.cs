namespace Subly.Application.Contracts;

public sealed record LoginUserRequest(
    string Email,
    string Password);

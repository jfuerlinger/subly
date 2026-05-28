namespace Subly.Application.Contracts;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

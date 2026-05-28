namespace Subly.Application.Contracts;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);

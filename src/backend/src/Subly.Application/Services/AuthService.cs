using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateNames(request.FirstName, request.LastName);
        ValidatePassword(request.Password);

        var normalizedEmail = User.NormalizeEmail(request.Email);
        var existing = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ArgumentException("A user with this email already exists.", nameof(request.Email));
        }

        var hashedPassword = passwordHasher.Hash(request.Password);
        var user = User.Create(
            request.FirstName,
            request.LastName,
            normalizedEmail,
            hashedPassword.HashBase64,
            hashedPassword.SaltBase64,
            hashedPassword.Iterations);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var authToken = tokenService.CreateToken(user);
        return ToAuthResponse(user, authToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.Password);

        var normalizedEmail = User.NormalizeEmail(request.Email);
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Email or password is invalid.");
        }

        var verified = passwordHasher.Verify(
            request.Password,
            new PasswordHashResult(user.PasswordHash, user.PasswordSalt, user.PasswordIterations));

        if (!verified)
        {
            throw new UnauthorizedAccessException("Email or password is invalid.");
        }

        var authToken = tokenService.CreateToken(user);
        return ToAuthResponse(user, authToken);
    }

    public async Task<AuthResponseDto> DevLoginAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("No user found for the given email.");
        }

        var authToken = tokenService.CreateToken(user);
        return ToAuthResponse(user, authToken);
    }

    private static AuthResponseDto ToAuthResponse(User user, AuthTokenResult authToken)
    {
        return new AuthResponseDto(
            authToken.AccessToken,
            authToken.ExpiresAtUtc,
            new AuthenticatedUserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email));
    }

    private static void ValidateNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        if (password.Length < 8)
        {
            throw new ArgumentException("Password must have at least 8 characters.", nameof(password));
        }
    }
}

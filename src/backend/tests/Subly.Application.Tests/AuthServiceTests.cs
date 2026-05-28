using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnToken()
    {
        var repository = new InMemoryUserRepository();
        var service = new AuthService(repository, new FakePasswordHasher(), new FakeTokenService());

        var result = await service.RegisterAsync(new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: "Max@Example.com",
            Password: "Secure123!"));

        result.AccessToken.Should().Be("token-value");
        result.User.Email.Should().Be("max@example.com");
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordDoesNotMatch()
    {
        var repository = new InMemoryUserRepository();
        repository.Items.Add(User.Create(
            firstName: "Max",
            lastName: "Muster",
            email: "max@example.com",
            passwordHash: "hash-expected",
            passwordSalt: "salt-value",
            passwordIterations: 100_000));

        var service = new AuthService(repository, new FakePasswordHasher(), new FakeTokenService());

        var action = async () => await service.LoginAsync(new LoginUserRequest(
            Email: "max@example.com",
            Password: "wrong-password"));

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHashResult Hash(string password)
        {
            return new PasswordHashResult($"hash-{password}", "salt-value", 100_000);
        }

        public bool Verify(string password, PasswordHashResult hashedPassword)
        {
            return hashedPassword.HashBase64 == $"hash-{password}";
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AuthTokenResult CreateToken(User user)
        {
            return new AuthTokenResult("token-value", new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero));
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.SingleOrDefault(x => x.Email == email));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

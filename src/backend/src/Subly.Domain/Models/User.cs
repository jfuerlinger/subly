namespace Subly.Domain.Models;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string PasswordSalt { get; private set; } = string.Empty;

    public int PasswordIterations { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string passwordSalt,
        int passwordIterations)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (string.IsNullOrWhiteSpace(passwordSalt))
        {
            throw new ArgumentException("Password salt is required.", nameof(passwordSalt));
        }

        if (passwordIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(passwordIterations), "Password iteration count must be greater than zero.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash.Trim(),
            PasswordSalt = passwordSalt.Trim(),
            PasswordIterations = passwordIterations,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (!normalized.Contains('@'))
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        return normalized;
    }
}

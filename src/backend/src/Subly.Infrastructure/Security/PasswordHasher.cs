using System.Security.Cryptography;
using Subly.Application.Abstractions;

namespace Subly.Infrastructure.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int IterationCount = 100_000;

    public PasswordHashResult Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, IterationCount, HashAlgorithmName.SHA256, HashSize);

        return new PasswordHashResult(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            IterationCount);
    }

    public bool Verify(string password, PasswordHashResult hashedPassword)
    {
        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(hashedPassword.SaltBase64);
            expectedHash = Convert.FromBase64String(hashedPassword.HashBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            hashedPassword.Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}

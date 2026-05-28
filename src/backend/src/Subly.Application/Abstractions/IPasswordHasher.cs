namespace Subly.Application.Abstractions;

public interface IPasswordHasher
{
    PasswordHashResult Hash(string password);

    bool Verify(string password, PasswordHashResult hashedPassword);
}

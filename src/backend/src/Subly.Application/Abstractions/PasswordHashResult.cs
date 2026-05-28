namespace Subly.Application.Abstractions;

public sealed record PasswordHashResult(
    string HashBase64,
    string SaltBase64,
    int Iterations);

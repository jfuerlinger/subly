using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Security;

internal sealed class JwtTokenService : ITokenService
{
    private const string JwtConfigPrefix = "Authentication:Jwt";

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly int _tokenLifetimeMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration[$"{JwtConfigPrefix}:Issuer"]
            ?? throw new InvalidOperationException($"Missing configuration key '{JwtConfigPrefix}:Issuer'.");
        _audience = configuration[$"{JwtConfigPrefix}:Audience"]
            ?? throw new InvalidOperationException($"Missing configuration key '{JwtConfigPrefix}:Audience'.");

        var signingKeyValue = configuration[$"{JwtConfigPrefix}:SigningKey"]
            ?? throw new InvalidOperationException($"Missing configuration key '{JwtConfigPrefix}:SigningKey'.");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));

        var tokenLifetime = configuration[$"{JwtConfigPrefix}:TokenLifetimeMinutes"];
        _tokenLifetimeMinutes = int.TryParse(tokenLifetime, out var parsedTokenLifetime) ? parsedTokenLifetime : 120;
        if (_tokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException($"Configuration key '{JwtConfigPrefix}:TokenLifetimeMinutes' must be greater than zero.");
        }
    }

    public AuthTokenResult CreateToken(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(_tokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
        };

        var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return new AuthTokenResult(token, expiresAtUtc);
    }
}

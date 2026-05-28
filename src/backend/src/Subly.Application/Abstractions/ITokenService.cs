using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface ITokenService
{
    AuthTokenResult CreateToken(User user);
}

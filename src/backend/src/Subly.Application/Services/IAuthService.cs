using Subly.Application.Contracts;

namespace Subly.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> DevLoginAsync(string email, CancellationToken cancellationToken = default);
}

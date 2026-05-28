using System.Net.Http.Json;
using System.Text.Json;
using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public sealed class AuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync("/api/auth/register", request, cancellationToken);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync("/api/auth/login", request, cancellationToken);
    }

    private async Task<AuthResponseDto?> PostAsync<TRequest>(string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AuthResponseDto>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Authentication error: {ex.Message}");
            return null;
        }
    }
}

using System.Text.Json;
using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public class AdminApiClient
{
    private readonly HttpClient _httpClient;

    public AdminApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DatabaseResetResultDto?> ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/admin/reset-database", content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DatabaseResetResultDto>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error resetting database: {ex.Message}");
            return null;
        }
    }
}

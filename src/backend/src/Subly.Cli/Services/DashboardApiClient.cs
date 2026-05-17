using System.Text.Json;
using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public class DashboardApiClient
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dashboard/summary", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DashboardSummaryDto>(content);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching dashboard summary: {ex.Message}");
            return null;
        }
    }
}

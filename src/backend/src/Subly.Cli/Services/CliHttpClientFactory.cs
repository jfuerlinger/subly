using System.Net.Http.Headers;

namespace Subly.Cli.Services;

internal static class CliHttpClientFactory
{
    public static HttpClient Create(string apiUrl, string? token)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }

        return httpClient;
    }
}

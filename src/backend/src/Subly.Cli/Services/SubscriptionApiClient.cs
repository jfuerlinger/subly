using System.Text.Json;
using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public class SubscriptionApiClient
{
    private readonly HttpClient _httpClient;

    public SubscriptionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SubscriptionDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/subscriptions", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<SubscriptionDto>>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching subscriptions: {ex.Message}");
            return null;
        }
    }

    public async Task<SubscriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/subscriptions/{id}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"Subscription with ID {id} not found");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SubscriptionDto>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching subscription: {ex.Message}");
            return null;
        }
    }

    public async Task<SubscriptionDto?> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/subscriptions", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SubscriptionDto>(responseContent, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating subscription: {ex.Message}");
            return null;
        }
    }

    public async Task<List<LogoSuggestionDto>?> SuggestLogosAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(name);
            var response = await _httpClient.GetAsync($"/api/subscriptions/logo-suggestions?name={encodedName}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<LogoSuggestionDto>>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching logo suggestions: {ex.Message}");
            return null;
        }
    }

    public async Task<SubscriptionDto?> UpdateStatusAsync(Guid id, UpdateSubscriptionStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var request2 = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/subscriptions/{id}/status")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request2, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"Subscription with ID {id} not found");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SubscriptionDto>(responseContent, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error updating subscription status: {ex.Message}");
            return null;
        }
    }

    public async Task<SubscriptionDto?> UpdateAsync(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/subscriptions/{id}", content, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"Subscription with ID {id} not found");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SubscriptionDto>(responseContent, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error updating subscription: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/subscriptions/{id}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"Subscription with ID {id} not found");
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting subscription: {ex.Message}");
            return false;
        }
    }
}

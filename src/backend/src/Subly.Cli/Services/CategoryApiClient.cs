using System.Text.Json;
using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public class CategoryApiClient
{
    private readonly HttpClient _httpClient;

    public CategoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CategoryDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/categories", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<CategoryDto>>(content, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching categories: {ex.Message}");
            return null;
        }
    }

    public async Task<CategoryDto?> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/categories", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CategoryDto>(responseContent, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating category: {ex.Message}");
            return null;
        }
    }

    public async Task<CategoryDto?> RenameAsync(Guid id, RenameCategoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/api/categories/{id}/name", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Error: {response.StatusCode}");
                Console.Error.WriteLine(errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CategoryDto>(responseContent, JsonSerializerOptionsProvider.Web);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error renaming category: {ex.Message}");
            return null;
        }
    }
}

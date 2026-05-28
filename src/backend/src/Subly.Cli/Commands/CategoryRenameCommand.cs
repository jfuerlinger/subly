using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("category-rename", HelpText = "Rename an existing category")]
public class CategoryRenameCommand
{
    [Option('i', "id", Required = true, HelpText = "Category ID (GUID)")]
    public string? Id { get; set; }

    [Option('n', "name", Required = true, HelpText = "New category name")]
    public string? Name { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            if (!Guid.TryParse(Id, out var categoryId))
            {
                Console.Error.WriteLine("Invalid category ID. Must be a valid GUID.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Console.Error.WriteLine("Category name cannot be empty.");
                return 1;
            }

            var request = new RenameCategoryRequest(Name);

            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new CategoryApiClient(httpClient);

            var category = await client.RenameAsync(categoryId, request);
            if (category == null)
                return 1;

            Console.WriteLine($"\n✓ Category renamed successfully!");
            Console.WriteLine($"ID:   {category.Id}");
            Console.WriteLine($"Name: {category.Name}\n");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

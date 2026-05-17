using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("category-create", HelpText = "Create a new category")]
public class CategoryCreateCommand
{
    [Option('n', "name", Required = true, HelpText = "Category name")]
    public string? Name { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Console.Error.WriteLine("Category name cannot be empty");
                return 1;
            }

            var request = new CreateCategoryRequest(Name);

            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new CategoryApiClient(httpClient);

            var category = await client.CreateAsync(request);
            if (category == null)
                return 1;

            Console.WriteLine($"\n✓ Category created successfully!");
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

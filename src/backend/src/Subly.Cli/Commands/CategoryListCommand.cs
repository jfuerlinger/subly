using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("category-list", HelpText = "List all categories")]
public class CategoryListCommand
{
    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new CategoryApiClient(httpClient);

            var categories = await client.GetAllAsync();
            if (categories == null)
                return 1;

            OutputFormatter.PrintCategoryTable(categories);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

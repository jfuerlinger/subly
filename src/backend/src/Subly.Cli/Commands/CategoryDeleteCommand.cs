using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("category-delete", HelpText = "Delete a category, optionally reassigning its subscriptions")]
public sealed class CategoryDeleteCommand
{
    [Option('i', "id", Required = true, HelpText = "Category ID (GUID)")]
    public string? Id { get; set; }

    [Option('r', "replacement-id", HelpText = "Replacement category ID (required when the category has subscriptions)")]
    public string? ReplacementId { get; set; }

    [Option('y', "yes", Default = false, HelpText = "Skip confirmation")]
    public bool SkipConfirmation { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    [Option('t', "token", HelpText = "JWT access token")]
    public string? Token { get; set; }

    public async Task<int> Execute()
    {
        if (!Guid.TryParse(Id, out var categoryId))
        {
            Console.Error.WriteLine("Invalid category ID. Must be a valid GUID.");
            return 1;
        }

        Guid? replacementCategoryId = null;
        if (!string.IsNullOrWhiteSpace(ReplacementId))
        {
            if (!Guid.TryParse(ReplacementId, out var parsedReplacementId))
            {
                Console.Error.WriteLine("Invalid replacement category ID. Must be a valid GUID.");
                return 1;
            }

            replacementCategoryId = parsedReplacementId;
        }

        if (!SkipConfirmation)
        {
            Console.Write($"Are you sure you want to delete category {categoryId}? (yes/no): ");
            if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Deletion cancelled.");
                return 0;
            }
        }

        using var httpClient = CliHttpClientFactory.Create(ApiUrl, Token);
        var client = new CategoryApiClient(httpClient);
        var deleted = await client.DeleteAsync(categoryId, new DeleteCategoryRequest(replacementCategoryId));
        if (!deleted)
        {
            return 1;
        }

        Console.WriteLine("✓ Category deleted successfully!");
        return 0;
    }
}

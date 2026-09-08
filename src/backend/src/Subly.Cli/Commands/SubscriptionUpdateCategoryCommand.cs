using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-update-category", HelpText = "Move a subscription to another category")]
public sealed class SubscriptionUpdateCategoryCommand
{
    [Value(0, MetaName = "id", HelpText = "The subscription ID")]
    public string? Id { get; set; }

    [Option('c', "category-id", Required = true, HelpText = "Target category ID")]
    public string? CategoryId { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    [Option('t', "token", HelpText = "JWT access token")]
    public string? Token { get; set; }

    public async Task<int> Execute()
    {
        if (!Guid.TryParse(Id, out var subscriptionId) || !Guid.TryParse(CategoryId, out var categoryId))
        {
            Console.Error.WriteLine("Invalid subscription or category ID format");
            return 1;
        }

        using var httpClient = CliHttpClientFactory.Create(ApiUrl, Token);
        var client = new SubscriptionApiClient(httpClient);
        var subscription = await client.UpdateCategoryAsync(subscriptionId, new UpdateSubscriptionCategoryRequest(categoryId));
        if (subscription is null)
        {
            return 1;
        }

        Console.WriteLine("\n✓ Subscription category updated successfully!");
        OutputFormatter.PrintSubscriptionDetail(subscription);
        return 0;
    }
}

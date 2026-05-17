using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-delete", HelpText = "Delete a subscription")]
public class SubscriptionDeleteCommand
{
    [Value(0, MetaName = "id", HelpText = "The subscription ID")]
    public string? Id { get; set; }

    [Option('y', "yes", Default = false, HelpText = "Skip confirmation")]
    public bool SkipConfirmation { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            if (!Guid.TryParse(Id, out var subscriptionId))
            {
                Console.Error.WriteLine("Invalid subscription ID format");
                return 1;
            }

            if (!SkipConfirmation)
            {
                Console.Write($"Are you sure you want to delete subscription {subscriptionId}? (yes/no): ");
                var confirmation = Console.ReadLine();

                if (confirmation?.ToLower() != "yes")
                {
                    Console.WriteLine("Deletion cancelled.");
                    return 0;
                }
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new SubscriptionApiClient(httpClient);

            var deleted = await client.DeleteAsync(subscriptionId);
            if (!deleted)
                return 1;

            Console.WriteLine("✓ Subscription deleted successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

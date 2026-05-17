using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-list", HelpText = "List all subscriptions")]
public class SubscriptionListCommand
{
    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new SubscriptionApiClient(httpClient);

            var subscriptions = await client.GetAllAsync();
            if (subscriptions == null)
                return 1;

            OutputFormatter.PrintSubscriptionTable(subscriptions);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-get", HelpText = "Get a subscription by ID")]
public class SubscriptionGetCommand
{
    [Value(0, MetaName = "id", HelpText = "The subscription ID")]
    public string? Id { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    [Option('t', "token", HelpText = "JWT access token")]
    public string? Token { get; set; }

    public async Task<int> Execute()
    {
        try
        {
            if (!Guid.TryParse(Id, out var subscriptionId))
            {
                Console.Error.WriteLine("Invalid subscription ID format");
                return 1;
            }

            using var httpClient = CliHttpClientFactory.Create(ApiUrl, Token);
            var client = new SubscriptionApiClient(httpClient);

            var subscription = await client.GetByIdAsync(subscriptionId);
            if (subscription == null)
                return 1;

            OutputFormatter.PrintSubscriptionDetail(subscription);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

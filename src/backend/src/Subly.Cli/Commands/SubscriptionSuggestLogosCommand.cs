using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-suggest-logos", HelpText = "Get logo suggestions for a subscription name")]
public class SubscriptionSuggestLogosCommand
{
    [Option('n', "name", Required = true, HelpText = "Subscription name to suggest logos for")]
    public string? Name { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    [Option('t', "token", HelpText = "JWT access token")]
    public string? Token { get; set; }

    public async Task<int> Execute()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Console.Error.WriteLine("Subscription name is required.");
            return 1;
        }

        try
        {
            using var httpClient = CliHttpClientFactory.Create(ApiUrl, Token);
            var client = new SubscriptionApiClient(httpClient);

            var suggestions = await client.SuggestLogosAsync(Name);
            if (suggestions is null)
            {
                return 1;
            }

            OutputFormatter.PrintLogoSuggestionTable(suggestions);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

using System.Globalization;
using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-update-status", HelpText = "Update subscription status")]
public class SubscriptionUpdateStatusCommand
{
    [Value(0, MetaName = "id", HelpText = "The subscription ID")]
    public string? Id { get; set; }

    [Option('s', "status", Required = true, HelpText = "New status (active, paused, cancelled)")]
    public string? Status { get; set; }

    [Option("cancelled", HelpText = "Cancellation date (yyyy-MM-dd)")]
    public string? CancelledAt { get; set; }

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

            DateOnly? cancelledAtValue = null;
            if (!string.IsNullOrWhiteSpace(CancelledAt))
            {
                if (!DateOnly.TryParseExact(CancelledAt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ca))
                {
                    Console.Error.WriteLine("Invalid cancellation date format. Use format: yyyy-MM-dd");
                    return 1;
                }
                cancelledAtValue = ca;
            }

            var request = new UpdateSubscriptionStatusRequest(Status ?? string.Empty, cancelledAtValue);

            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new SubscriptionApiClient(httpClient);

            var subscription = await client.UpdateStatusAsync(subscriptionId, request);
            if (subscription == null)
                return 1;

            Console.WriteLine("\n✓ Subscription status updated successfully!");
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

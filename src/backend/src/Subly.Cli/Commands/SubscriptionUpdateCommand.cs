using System.Globalization;
using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("subscription-update", HelpText = "Update an existing subscription")]
public class SubscriptionUpdateCommand
{
    [Value(0, MetaName = "id", HelpText = "The subscription ID")]
    public string? Id { get; set; }

    [Option('n', "name", Required = true, HelpText = "Subscription name")]
    public string? Name { get; set; }

    [Option('v', "vendor", Required = true, HelpText = "Vendor name")]
    public string? Vendor { get; set; }

    [Option('c', "category-id", Required = true, HelpText = "Category ID (see category-list)")]
    public string? CategoryId { get; set; }

    [Option('p', "price", Required = true, HelpText = "Price in euros")]
    public string? Price { get; set; }

    [Option('l', "cycle", Required = true, HelpText = "Billing cycle (monthly, yearly, quarterly)")]
    public string? Cycle { get; set; }

    [Option("next-payment", Required = true, HelpText = "Next payment date (yyyy-MM-dd)")]
    public string? NextPaymentDate { get; set; }

    [Option("payment-method", Required = true, HelpText = "Payment method")]
    public string? PaymentMethod { get; set; }

    [Option("started", Required = true, HelpText = "Start date (yyyy-MM-dd)")]
    public string? StartedAt { get; set; }

    [Option("cancelled", HelpText = "Cancellation date (yyyy-MM-dd)")]
    public string? CancelledAt { get; set; }

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

            if (!Guid.TryParse(CategoryId, out var categoryIdValue))
            {
                Console.Error.WriteLine("Invalid category ID format");
                return 1;
            }

            if (!decimal.TryParse(Price, CultureInfo.InvariantCulture, out var priceValue))
            {
                Console.Error.WriteLine("Invalid price format. Use format like: 15.99");
                return 1;
            }

            if (!DateOnly.TryParseExact(NextPaymentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var nextPaymentDateValue))
            {
                Console.Error.WriteLine("Invalid next payment date format. Use format: yyyy-MM-dd");
                return 1;
            }

            if (!DateOnly.TryParseExact(StartedAt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startedAtValue))
            {
                Console.Error.WriteLine("Invalid start date format. Use format: yyyy-MM-dd");
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

            var request = new UpdateSubscriptionRequest(
                Name ?? string.Empty,
                Vendor ?? string.Empty,
                categoryIdValue,
                priceValue,
                Cycle ?? string.Empty,
                nextPaymentDateValue,
                PaymentMethod ?? string.Empty,
                startedAtValue,
                cancelledAtValue);

            using var httpClient = CliHttpClientFactory.Create(ApiUrl, Token);
            var client = new SubscriptionApiClient(httpClient);

            var subscription = await client.UpdateAsync(subscriptionId, request);
            if (subscription == null)
            {
                return 1;
            }

            Console.WriteLine("\n✓ Subscription updated successfully!");
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

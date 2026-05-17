using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("dashboard-summary", HelpText = "Get dashboard summary")]
public class DashboardSummaryCommand
{
    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new DashboardApiClient(httpClient);

            var summary = await client.GetSummaryAsync();
            if (summary == null)
                return 1;

            OutputFormatter.PrintDashboardSummary(summary);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

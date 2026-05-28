using CommandLine;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("database-reset", HelpText = "Reset database, re-apply migrations and seed data")]
public class DatabaseResetCommand
{
    [Option('y', "yes", Default = false, HelpText = "Skip confirmation")]
    public bool SkipConfirmation { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            if (!SkipConfirmation)
            {
                Console.Write("Are you sure you want to reset the database? This action cannot be undone. (yes/no): ");
                var confirmation = Console.ReadLine();

                if (confirmation?.ToLowerInvariant() != "yes")
                {
                    Console.WriteLine("Database reset cancelled.");
                    return 0;
                }
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var client = new AdminApiClient(httpClient);

            var result = await client.ResetDatabaseAsync();
            if (result == null)
                return 1;

            Console.WriteLine("✓ Database reset completed successfully!");
            Console.WriteLine($"Completed at: {result.CompletedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("Executed steps:");
            for (var index = 0; index < result.Steps.Count; index++)
            {
                Console.WriteLine($"{index + 1}. {result.Steps[index]}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

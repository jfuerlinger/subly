using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("auth-register", HelpText = "Register a user and return an access token")]
public sealed class AuthRegisterCommand
{
    [Option("first-name", Required = true, HelpText = "First name")]
    public string? FirstName { get; set; }

    [Option("last-name", Required = true, HelpText = "Last name")]
    public string? LastName { get; set; }

    [Option('e', "email", Required = true, HelpText = "Email address")]
    public string? Email { get; set; }

    [Option('p', "password", Required = true, HelpText = "Password (minimum 8 characters)")]
    public string? Password { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            var request = new RegisterUserRequest(
                FirstName ?? string.Empty,
                LastName ?? string.Empty,
                Email ?? string.Empty,
                Password ?? string.Empty);

            using var httpClient = CliHttpClientFactory.Create(ApiUrl, null);
            var client = new AuthApiClient(httpClient);

            var response = await client.RegisterAsync(request);
            if (response is null)
            {
                return 1;
            }

            OutputFormatter.PrintAuthResponse(response);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

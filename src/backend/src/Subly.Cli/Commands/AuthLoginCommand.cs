using CommandLine;
using Subly.Cli.Contracts;
using Subly.Cli.Services;

namespace Subly.Cli.Commands;

[Verb("auth-login", HelpText = "Log in a user and return an access token")]
public sealed class AuthLoginCommand
{
    [Option('e', "email", Required = true, HelpText = "Email address")]
    public string? Email { get; set; }

    [Option('p', "password", Required = true, HelpText = "Password")]
    public string? Password { get; set; }

    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Base URL for the Subly API")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            var request = new LoginUserRequest(
                Email ?? string.Empty,
                Password ?? string.Empty);

            using var httpClient = CliHttpClientFactory.Create(ApiUrl, null);
            var client = new AuthApiClient(httpClient);

            var response = await client.LoginAsync(request);
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

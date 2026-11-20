using CommandLine;
using ModelContextProtocol.Client;

namespace Subly.Cli.Commands;

[Verb("mcp-tools", HelpText = "List the tools offered by the Subly MCP endpoint")]
public sealed class McpToolsCommand
{
    [Option('u', "api-url", Default = "http://localhost:5000", HelpText = "Subly base URL")]
    public string ApiUrl { get; set; } = string.Empty;

    public async Task<int> Execute()
    {
        try
        {
            var endpoint = new Uri(new Uri(ApiUrl), "/mcp");
            var token = Environment.GetEnvironmentVariable("SUBLY_MCP_ACCESS_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Set SUBLY_MCP_ACCESS_TOKEN to a Cloudflare OAuth access token.");
            if (endpoint.Scheme != Uri.UriSchemeHttps && !endpoint.IsLoopback)
                throw new ArgumentException("Use HTTPS for a remote MCP endpoint.");

            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            }, http);
            await using var client = await McpClient.CreateAsync(transport);
            foreach (var tool in await client.ListToolsAsync())
                Console.WriteLine($"{tool.Name}  {tool.Description}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

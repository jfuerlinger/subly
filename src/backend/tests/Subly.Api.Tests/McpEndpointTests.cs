using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Subly.Infrastructure.Persistence;
using Subly.Application.Contracts;

namespace Subly.Api.Tests;

public sealed class McpEndpointTests : IDisposable
{
    private const string Issuer = "https://team.example.com";
    private const string Audience = "subly-mcp";
    private const string Email = "owner@example.com";
    private readonly RSA key = RSA.Create(2048);
    private readonly CustomWebApplicationFactory baseFactory = new();
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory;

    public McpEndpointTests()
    {
        var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
        discovery.SigningKeys.Add(new RsaSecurityKey(key) { KeyId = "mcp-test" });
        factory = baseFactory.WithWebHostBuilder(host => host.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>("SublyMcpAccess", options =>
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery))));
        var config = factory.Services.GetRequiredService<IConfiguration>();
        config["McpAccess:Issuer"] = Issuer;
        config["McpAccess:Audience"] = Audience;
        config["McpAccess:Email"] = Email;
        config["McpAccess:Subject"] = "owner-subject";
        config["McpAccess:UserId"] = Guid.NewGuid().ToString();
    }

    [Theory]
    [InlineData(null, Audience, Email, "owner-subject")]
    [InlineData("header", "kur-mcp", Email, "owner-subject")]
    [InlineData("header", Audience, "other@example.com", "owner-subject")]
    [InlineData("header", Audience, null, "another-subject")]
    [InlineData("bearer", Audience, Email, "owner-subject")]
    public async Task RejectsUnlinkedOrMissingAssertion(string? channel, string audience, string? email, string subject)
    {
        using var http = factory.CreateClient();
        var token = Sign(audience, email, subject);
        if (channel == "header") http.DefaultRequestHeaders.Add("Cf-Access-Jwt-Assertion", token);
        if (channel == "bearer") http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await http.PostAsJsonAsync("/mcp", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ManagesOnlyLinkedAccountsSubscriptions()
    {
        using var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("Cf-Access-Jwt-Assertion", Sign(Audience, null, "owner-subject"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        using var web = factory.CreateClient();
        var registration = await web.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest("MCP", "Owner", Email, "Testing1234!"));
        var account = await registration.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(account);
        factory.Services.GetRequiredService<IConfiguration>()["McpAccess:UserId"] = account.User.Id.ToString();
        web.DefaultRequestHeaders.Authorization = new("Bearer", account.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await web.PostAsJsonAsync("/mcp", new { })).StatusCode);
        var foreignId = db.Subscriptions.First().Id;
        var existingCategoryId = db.Categories.Select(category => category.Id).First();

        // Use a real SDK client through HTTP, covering initialization, discovery and calls.
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }, http);
        await using var client = await McpClient.CreateAsync(transport);
        var tools = await client.ListToolsAsync();
        Assert.Contains(tools, t => t.Name == "category_create");
        Assert.True(tools.Single(t => t.Name == "subscription_delete").ProtocolTool.Annotations?.DestructiveHint);
        Assert.True(tools.Single(t => t.Name == "subscription_list").ProtocolTool.Annotations?.ReadOnlyHint);

        ValueTask<CallToolResult> Call(string name, Dictionary<string, object?>? args = null) => client.CallToolAsync(name, args);
        static string Text(CallToolResult result) => Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        static JsonElement Record(CallToolResult result) => JsonSerializer.Deserialize<JsonElement>(Text(result));

        Assert.True((await Call("subscription_get", new() { ["id"] = foreignId })).IsError);
        Assert.True((await Call("subscription_delete", new() { ["id"] = foreignId })).IsError);
        var category = Record(await Call("category_create", new() { ["name"] = "  Weiterbildung  " }));
        var categoryId = category.GetProperty("id").GetGuid();
        Assert.Equal("Weiterbildung", category.GetProperty("name").GetString());
        Assert.Contains(categoryId.ToString(), Text(await Call("category_list")));
        Assert.True((await Call("category_create", new() { ["name"] = "weiterbildung" })).IsError);
        Assert.True((await Call("category_create", new() { ["name"] = "  " })).IsError);
        var details = new Dictionary<string, object?>
        {
            ["name"] = "Test subscription", ["vendor"] = "Test vendor", ["cycle"] = "monthly",
            ["categoryId"] = categoryId, ["price"] = 8.50m, ["startedAt"] = "2026-10-01",
            ["nextPaymentDate"] = "2026-12-01", ["paymentMethod"] = "transfer", ["cancelledAt"] = null,
        };
        var created = Record(await Call("subscription_create", new() { ["subscription"] = details }));
        var id = created.GetProperty("id").GetGuid();
        Assert.Contains(id.ToString(), Text(await Call("subscription_list")));
        Assert.DoesNotContain(foreignId.ToString(), Text(await Call("subscription_list")));
        Assert.Equal(categoryId, created.GetProperty("categoryId").GetGuid());
        Assert.Equal(1, Record(await Call("dashboard_summary")).GetProperty("activeSubscriptionsCount").GetInt32());
        details["price"] = -1;
        Assert.True((await Call("subscription_update", new() { ["id"] = id, ["subscription"] = details })).IsError);
        details["price"] = 10;
        Assert.Equal(10m, Record(await Call("subscription_update", new() { ["id"] = id, ["subscription"] = details })).GetProperty("price").GetDecimal());
        Assert.Equal("paused", Record(await Call("subscription_set_status", new() { ["id"] = id, ["status"] = "paused" })).GetProperty("status").GetString());
        Assert.Equal(existingCategoryId, Record(await Call("subscription_set_category", new() { ["id"] = id, ["categoryId"] = existingCategoryId })).GetProperty("categoryId").GetGuid());
        Assert.Equal(id, Record(await Call("subscription_get", new() { ["id"] = id })).GetProperty("id").GetGuid());
        Assert.NotEqual(true, (await Call("subscription_delete", new() { ["id"] = id })).IsError);
        Assert.True((await Call("subscription_get", new() { ["id"] = id })).IsError);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/subscriptions")).StatusCode);
    }

    private string Sign(string audience, string? email, string subject)
    {
        var claims = new List<Claim> { new("sub", subject) };
        if (email != null) claims.Add(new Claim("email", email));
        var credentials = new SigningCredentials(new RsaSecurityKey(key) { KeyId = "mcp-test" }, SecurityAlgorithms.RsaSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(Issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials));
    }

    public void Dispose()
    {
        factory.Dispose();
        baseFactory.Dispose();
        key.Dispose();
    }
}

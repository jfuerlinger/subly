using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;

namespace Subly.Api.Mcp;

internal static class McpAccess
{
    public const string Scheme = "SublyMcpAccess";

    public static void AddSubscriptionMcp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication().AddJwtBearer(Scheme, options =>
        {
            var access = configuration.GetSection("McpAccess");
            options.Authority = access["Issuer"];
            options.Audience = access["Audience"];
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var origins = context.Request.Headers.Origin;
                    if (origins.Count > 0 && (origins.Count != 1 || !string.Equals(origins[0],
                        $"https://{context.Request.Host}", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Fail("Invalid MCP origin.");
                        return Task.CompletedTask;
                    }
                    var assertions = context.Request.Headers["Cf-Access-Jwt-Assertion"];
                    if (assertions.Count == 1 && !string.IsNullOrWhiteSpace(assertions[0])
                        && !string.IsNullOrWhiteSpace(options.Authority)
                        && !string.IsNullOrWhiteSpace(options.Audience))
                    {
                        context.Token = assertions[0];
                    }
                    else
                    {
                        // Only the assertion forwarded by Access can authenticate this endpoint.
                        context.NoResult();
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var subject = context.Principal?.FindFirstValue("sub");
                    var email = context.Principal?.FindFirstValue("email");
                    var expectedEmail = access["Email"];
                    // Managed OAuth can omit email. Only accept an explicitly bound subject then.
                    var matches = string.IsNullOrEmpty(email)
                        ? subject == access["Subject"]
                        : string.Equals(email, expectedEmail, StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrWhiteSpace(subject) || !matches
                        || !Guid.TryParse(access["UserId"], out var userId) || userId == Guid.Empty)
                    {
                        context.Fail("No Subly account is linked to this Access identity.");
                    }
                    else
                    {
                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], Scheme));
                    }
                    return Task.CompletedTask;
                },
            };
        });
        var json = new JsonSerializerOptions(JsonSerializerOptions.Web)
        {
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        };
        services.AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithTools<SubscriptionTools>(json);
    }
}

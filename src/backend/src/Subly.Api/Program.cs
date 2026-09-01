using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Subly.Api.Authentication;
using Subly.Application.Abstractions;
using Subly.Application.Services;
using Subly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
const string developmentJwtSigningKey = "subly-development-only-jwt-signing-key-not-for-production";

builder.AddServiceDefaults();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();

var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        jwtSigningKey = developmentJwtSigningKey;
        builder.Configuration["Authentication:Jwt:SigningKey"] = jwtSigningKey;
    }
    else
    {
        throw new InvalidOperationException("Missing configuration key 'Authentication:Jwt:SigningKey'.");
    }
}
var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"]
    ?? throw new InvalidOperationException("Missing configuration key 'Authentication:Jwt:Issuer'.");
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"]
    ?? throw new InvalidOperationException("Missing configuration key 'Authentication:Jwt:Audience'.");
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (args.Contains("--reset-db", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        throw new InvalidOperationException("--reset-db is not allowed in this environment.");
    }

    await app.Services.ResetDatabaseAsync();
    return;
}

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.EnsureDatabaseInitializedAsync(seed: true);
    return;
}

await app.Services.EnsureDatabaseInitializedAsync(seed: true);

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

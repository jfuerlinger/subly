using System.Text.Json;
using System.Text.Json.Serialization;
using Subly.Application.Services;
using Subly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.EnsureDatabaseInitializedAsync(seed: true);

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.EnsureDatabaseInitializedAsync(seed: true);
    return;
}

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;

using System.Text.Json;
using System.Text.Json.Serialization;
using Subly.Application.Services;
using Subly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (args.Contains("--reset-db", StringComparer.OrdinalIgnoreCase))
{
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
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

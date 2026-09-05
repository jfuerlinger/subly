using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Contracts;
using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;

namespace Subly.Api.Tests;

public sealed class AdminEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task ResetDatabase_ShouldReturn200AndIncludeStepSummary()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/admin/reset-database", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resetSummary = await response.Content.ReadFromJsonAsync<DatabaseResetResultDto>(JsonOptions);
        resetSummary.Should().NotBeNull();
        resetSummary!.Steps.Should().ContainInOrder(
            "Vorhandene Datenbank gelöscht",
            "Migrationen erneut angewendet",
            "Seed-Daten neu eingespielt");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        dbContext.Subscriptions.Any().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAllData_ShouldReturn204AndLeaveEmptyRepository()
    {
        var client = factory.CreateClient();
        var authClient = await CreateAuthenticatedClientAsync(client, $"admin-delete-{Guid.NewGuid():N}@example.com");
        var categories = await authClient.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories", JsonOptions);
        await authClient.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Cleanup",
            Vendor: "OpenAI",
            CategoryId: categories!.Single(c => c.Name == "software").Id,
            Price: 20m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: new DateOnly(2026, 5, 20),
            PaymentMethod: "Visa",
            StartedAt: new DateOnly(2026, 5, 1),
            CancelledAt: null));

        var response = await client.DeleteAsync("/api/admin/data");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var subscriptions = await authClient.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        subscriptions.Should().NotBeNull();
        subscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedData_ShouldReturn204AndPopulateRepository()
    {
        var client = factory.CreateClient();

        // Start from empty state
        await client.DeleteAsync("/api/admin/data");

        var response = await client.PostAsync("/api/admin/seed", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();
        dbContext.Subscriptions.Any().Should().BeTrue();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(HttpClient client, string email)
    {
        var secret = $"Auth-{Guid.NewGuid():N}!";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Admin",
            LastName: "Test",
            Email: email,
            Password: secret));
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

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

        var subscriptions = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        subscriptions.Should().NotBeNull();
        subscriptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAllData_ShouldReturn204AndLeaveEmptyRepository()
    {
        var client = factory.CreateClient();
        var authClient = await CreateAuthenticatedClientAsync(client, $"admin-delete-{Guid.NewGuid():N}@example.com", "Secure123!");
        await authClient.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Cleanup",
            Vendor: "OpenAI",
            Category: "software",
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

        var demoUserClient = factory.CreateClient();
        var loginResponse = await demoUserClient.PostAsJsonAsync("/api/auth/login", new LoginUserRequest(
            Email: "demo@subly.local",
            Password: "SublyDemo123!"));
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        demoUserClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var subscriptions = await demoUserClient.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        subscriptions.Should().NotBeNull();
        subscriptions.Should().NotBeEmpty();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Admin",
            LastName: "Test",
            Email: email,
            Password: password));
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}

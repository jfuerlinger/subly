using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Subly.Application.Contracts;

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

        var response = await client.DeleteAsync("/api/admin/data");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var subscriptions = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
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

        var subscriptions = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        subscriptions.Should().NotBeNull();
        subscriptions.Should().NotBeEmpty();
    }
}

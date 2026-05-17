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

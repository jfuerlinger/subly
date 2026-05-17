using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Api.Tests;

public sealed class SubscriptionEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task GetSubscriptions_ShouldReturnSeededSubscriptions()
    {
        var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSubscription_ShouldCreateAndReturnCreatedEntity()
    {
        var client = factory.CreateClient();
        var request = new CreateSubscriptionRequest(
            Name: "ChatGPT Plus",
            Vendor: "OpenAI",
            Category: "software",
            Price: 22m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: new DateOnly(2026, 5, 20),
            PaymentMethod: "Visa",
            StartedAt: new DateOnly(2026, 3, 10),
            CancelledAt: null);

        var response = await client.PostAsJsonAsync("/api/subscriptions", request);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Name.Should().Be("ChatGPT Plus");
    }

    [Fact]
    public async Task UpdateStatus_ShouldUpdateExistingSubscription()
    {
        var client = factory.CreateClient();
        var subscriptions = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        var targetId = subscriptions!.First().Id;

        var response = await client.PatchAsJsonAsync($"/api/subscriptions/{targetId}/status", new UpdateSubscriptionStatusRequest(SubscriptionStatus.Paused, null));
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SubscriptionStatus.Paused);
    }

    [Fact]
    public async Task UpdateStatus_ShouldPersistCancellationDate_WhenCancelled()
    {
        var client = factory.CreateClient();
        var subscriptions = await client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        var targetId = subscriptions!.First().Id;
        var cancellationDate = new DateOnly(2026, 5, 15);

        var response = await client.PatchAsJsonAsync($"/api/subscriptions/{targetId}/status", new UpdateSubscriptionStatusRequest(SubscriptionStatus.Cancelled, cancellationDate));
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SubscriptionStatus.Cancelled);
        updated.CancelledAt.Should().Be(cancellationDate);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldReturnCalculatedSummary()
    {
        var client = factory.CreateClient();

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", JsonOptions);

        summary.Should().NotBeNull();
        summary!.ActiveSubscriptionsCount.Should().BeGreaterThan(0);
        summary.MonthlyTotal.Should().BeGreaterThan(0m);
    }
}

using System.Net;
using System.Net.Http.Headers;
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
    public async Task GetSubscriptions_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAndGetSubscriptions_ShouldReturnOnlyCurrentUserSubscriptions()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var user1Client = await CreateAuthenticatedClientAsync($"user1-{uniqueSuffix}@example.com");
        var user2Client = await CreateAuthenticatedClientAsync($"user2-{uniqueSuffix}@example.com");

        var request1 = CreateSubscriptionRequest($"Netflix-{uniqueSuffix}");
        var request2 = CreateSubscriptionRequest($"Spotify-{uniqueSuffix}");

        var createUser1Response = await user1Client.PostAsJsonAsync("/api/subscriptions", request1);
        var createUser2Response = await user2Client.PostAsJsonAsync("/api/subscriptions", request2);

        createUser1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        createUser2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user1Subscriptions = await user1Client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        var user2Subscriptions = await user2Client.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);

        user1Subscriptions.Should().NotBeNull();
        user2Subscriptions.Should().NotBeNull();

        var user1Names = user1Subscriptions!.Select(x => x.Name);
        var user2Names = user2Subscriptions!.Select(x => x.Name);

        user1Names.Should().Contain(request1.Name);
        user1Names.Should().NotContain(request2.Name);
        user2Names.Should().Contain(request2.Name);
        user2Names.Should().NotContain(request1.Name);
    }

    [Fact]
    public async Task UpdateStatus_ShouldUpdateExistingSubscription()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var client = await CreateAuthenticatedClientAsync($"status-{uniqueSuffix}@example.com");
        var createResponse = await client.PostAsJsonAsync("/api/subscriptions", CreateSubscriptionRequest($"Notion-{uniqueSuffix}"));
        var created = await createResponse.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOptions);

        var response = await client.PatchAsJsonAsync(
            $"/api/subscriptions/{created!.Id}/status",
            new UpdateSubscriptionStatusRequest(SubscriptionStatus.Paused, null));
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SubscriptionStatus.Paused);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldReturnCalculatedSummaryForCurrentUser()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var client = await CreateAuthenticatedClientAsync($"summary-{uniqueSuffix}@example.com");
        await client.PostAsJsonAsync("/api/subscriptions", CreateSubscriptionRequest($"Summary-{uniqueSuffix}"));

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", JsonOptions);

        summary.Should().NotBeNull();
        summary!.ActiveSubscriptionsCount.Should().BeGreaterThan(0);
        summary.MonthlyTotal.Should().BeGreaterThan(0m);
    }

    private static CreateSubscriptionRequest CreateSubscriptionRequest(string name)
    {
        return new CreateSubscriptionRequest(
            Name: name,
            Vendor: "OpenAI",
            Category: "software",
            Price: 22m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: new DateOnly(2026, 5, 20),
            PaymentMethod: "Visa",
            StartedAt: new DateOnly(2026, 3, 10),
            CancelledAt: null);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: email,
            Password: "Secure123!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        return client;
    }
}

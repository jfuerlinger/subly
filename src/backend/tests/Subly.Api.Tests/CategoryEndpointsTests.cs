using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Api.Tests;

public sealed class CategoryEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Theory]
    [InlineData("GET", "/api/categories")]
    [InlineData("POST", "/api/categories")]
    [InlineData("PATCH", "/api/categories/00000000-0000-0000-0000-000000000001/name")]
    [InlineData("DELETE", "/api/categories/00000000-0000-0000-0000-000000000001")]
    public async Task Categories_ShouldReturnUnauthorized_WhenNoTokenIsProvided(string method, string path)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method is "POST" or "PATCH" ? JsonContent.Create(new { name = "anonymous" }) : null,
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnSeededCategories()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");

        var result = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories", JsonOptions);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result!.Select(c => c.Name).Should().Contain("Streaming");
    }

    [Fact]
    public async Task CreateCategory_ShouldCreateAndReturnNewCategory()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var request = new CreateCategoryRequest("gaming");

        var response = await client.PostAsJsonAsync("/api/categories", request);
        var body = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Name.Should().Be("gaming");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnBadRequest_WhenCategoryAlreadyExists()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var request = new CreateCategoryRequest("streaming");

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameCategory_ShouldRenameAndReturnUpdatedCategory()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");

        // First create a category to rename
        var created = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("renameme"));
        var body = await created.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        body.Should().NotBeNull();

        var renameResponse = await client.PatchAsJsonAsync($"/api/categories/{body!.Id}/name", new { name = "renamed" });
        var renamed = await renameResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);

        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        renamed.Should().NotBeNull();
        renamed!.Name.Should().Be("renamed");
        renamed.Id.Should().Be(body.Id);
    }

    [Fact]
    public async Task RenameCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var nonExistentId = Guid.NewGuid();

        var response = await client.PatchAsJsonAsync($"/api/categories/{nonExistentId}/name", new { name = "anything" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ShouldDeleteUnusedCategory()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var created = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"unused-{Guid.NewGuid():N}"));
        var category = await created.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{category!.Id}");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var categories = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories", JsonOptions);
        categories.Should().NotContain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReassignSubscriptionsToReplacementCategory()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var sourceResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"source-{Guid.NewGuid():N}"));
        var targetResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"target-{Guid.NewGuid():N}"));
        var source = await sourceResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        var target = await targetResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        var authenticatedClient = await CreateAuthenticatedClientAsync($"category-{Guid.NewGuid():N}@example.com");

        var subscriptionResponse = await authenticatedClient.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Reassign me",
            Vendor: "Subly",
            CategoryId: source!.Id,
            Price: 10m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: new DateOnly(2026, 10, 1),
            PaymentMethod: "Visa",
            StartedAt: new DateOnly(2026, 1, 1),
            CancelledAt: null));
        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{source.Id}")
        {
            Content = JsonContent.Create(new DeleteCategoryRequest(target!.Id)),
        };
        var deleteResponse = await client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var subscriptions = await authenticatedClient.GetFromJsonAsync<IReadOnlyList<SubscriptionDto>>("/api/subscriptions", JsonOptions);
        subscriptions.Should().ContainSingle();
        subscriptions!.Single().CategoryId.Should().Be(target.Id);
    }

    [Fact]
    public async Task DeleteCategory_ShouldRequireReplacement_WhenCategoryHasSubscriptions()
    {
        var client = await CreateAuthenticatedClientAsync($"admin-{Guid.NewGuid():N}@example.com");
        var sourceResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"source-{Guid.NewGuid():N}"));
        var source = await sourceResponse.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        var authenticatedClient = await CreateAuthenticatedClientAsync($"category-{Guid.NewGuid():N}@example.com");

        await authenticatedClient.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Keep me",
            Vendor: "Subly",
            CategoryId: source!.Id,
            Price: 10m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: new DateOnly(2026, 10, 1),
            PaymentMethod: "Visa",
            StartedAt: new DateOnly(2026, 1, 1),
            CancelledAt: null));

        var response = await client.DeleteAsync($"/api/categories/{source.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}

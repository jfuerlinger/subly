using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Subly.Application.Contracts;

namespace Subly.Api.Tests;

public sealed class CategoryEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task GetCategories_ShouldReturnSeededCategories()
    {
        var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories", JsonOptions);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result!.Select(c => c.Name).Should().Contain("streaming");
    }

    [Fact]
    public async Task CreateCategory_ShouldCreateAndReturnNewCategory()
    {
        var client = factory.CreateClient();
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
        var client = factory.CreateClient();
        var request = new CreateCategoryRequest("streaming");

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

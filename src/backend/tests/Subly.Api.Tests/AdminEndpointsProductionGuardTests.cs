using System.Net;
using FluentAssertions;

namespace Subly.Api.Tests;

public sealed class AdminEndpointsProductionGuardTests(ProductionWebApplicationFactory factory)
    : IClassFixture<ProductionWebApplicationFactory>
{
    [Fact]
    public async Task ResetDatabase_ShouldReturn403InProduction()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/admin/reset-database", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllData_ShouldReturn403InProduction()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/admin/data");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SeedData_ShouldReturn403InProduction()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/admin/seed", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

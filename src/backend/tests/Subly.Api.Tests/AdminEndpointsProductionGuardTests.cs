using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Contracts;
using Subly.Infrastructure.Persistence;

namespace Subly.Api.Tests;

public sealed class AdminEndpointsProductionGuardTests(ProductionWebApplicationFactory factory)
    : IClassFixture<ProductionWebApplicationFactory>
{
    [Fact]
    public async Task Register_ShouldReturn403InProductionByDefault()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: $"prod-{Guid.NewGuid():N}@example.com",
            Password: "Secure123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public void Startup_ShouldSeedCategoriesButNoDemoDataInProduction()
    {
        _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SublyDbContext>();

        dbContext.Categories.Should().NotBeEmpty();
        dbContext.Users.Should().BeEmpty();
        dbContext.Subscriptions.Should().BeEmpty();
    }

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

    [Fact]
    public async Task DevLogin_ShouldReturn403InProduction()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/dev-login", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

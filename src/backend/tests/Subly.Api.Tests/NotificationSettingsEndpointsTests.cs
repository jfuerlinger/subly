using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Subly.Application.Contracts;

namespace Subly.Api.Tests;

public sealed class NotificationSettingsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetNotificationSettings_ShouldReturnDefaults_ForNewUser()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var result = await client.GetFromJsonAsync<NotificationSettingsDto>("/api/notification-settings");

        result.Should().NotBeNull();
        result!.LeadDays.Should().Be(3);
        result.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotificationSettings_ShouldPersistAndReturnUpdatedSettings()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var response = await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(7, true));
        var body = await response.Content.ReadFromJsonAsync<NotificationSettingsDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.LeadDays.Should().Be(7);
        body.EmailEnabled.Should().BeTrue();

        var refetched = await client.GetFromJsonAsync<NotificationSettingsDto>("/api/notification-settings");
        refetched!.LeadDays.Should().Be(7);
        refetched.EmailEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotificationSettings_ShouldReturnBadRequest_WhenLeadDaysOutOfRange()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var response = await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(91, true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNotificationSettings_ShouldRequireAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notification-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}

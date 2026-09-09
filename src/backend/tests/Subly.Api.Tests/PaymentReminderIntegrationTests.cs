using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Api.Tests;

public sealed class PaymentReminderIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSendEmailOnce_ForSubscriptionDueAtConfiguredLeadTime()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var email = $"reminder-{uniqueSuffix}@example.com";
        var client = await CreateAuthenticatedClientAsync(email);
        var categories = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        (await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(3, true))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Reminder Test",
            Vendor: "Subly",
            CategoryId: categoryId,
            Price: 9.99m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: today.AddDays(3),
            PaymentMethod: "Visa",
            StartedAt: today.AddMonths(-1),
            CancelledAt: null))).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var reminderService = scope.ServiceProvider.GetRequiredService<IPaymentReminderService>();
            await reminderService.ProcessDueRemindersAsync();
            await reminderService.ProcessDueRemindersAsync();
        }

        factory.EmailSender.SentMessages.Should().ContainSingle(m => m.ToEmail == email);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: email,
            Password: "Secure123!"));
        registerResponse.EnsureSuccessStatusCode();

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}

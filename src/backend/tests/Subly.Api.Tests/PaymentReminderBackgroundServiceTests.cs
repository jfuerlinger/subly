using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Subly.Api.BackgroundServices;
using Subly.Application.Services;

namespace Subly.Api.Tests;

public sealed class PaymentReminderBackgroundServiceTests
{
    [Fact]
    public async Task RunOnceAsync_ShouldInvokePaymentReminderService()
    {
        var fakeReminderService = new FakePaymentReminderService();
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentReminderService>(fakeReminderService);
        var provider = services.BuildServiceProvider();
        var backgroundService = new PaymentReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PaymentReminderBackgroundService>.Instance);

        await backgroundService.RunOnceAsync(CancellationToken.None);

        fakeReminderService.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task RunOnceAsync_ShouldNotThrow_WhenReminderServiceFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentReminderService>(new ThrowingPaymentReminderService());
        var provider = services.BuildServiceProvider();
        var backgroundService = new PaymentReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PaymentReminderBackgroundService>.Instance);

        var act = () => backgroundService.RunOnceAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private sealed class FakePaymentReminderService : IPaymentReminderService
    {
        public int CallCount { get; private set; }

        public Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPaymentReminderService : IPaymentReminderService
    {
        public Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}

using Subly.Application.Services;

namespace Subly.Api.BackgroundServices;

public sealed class PaymentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IPaymentReminderService>();
            await reminderService.ProcessDueRemindersAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process due payment reminders.");
        }
    }
}

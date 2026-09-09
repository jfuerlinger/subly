namespace Subly.Application.Services;

public interface IPaymentReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}

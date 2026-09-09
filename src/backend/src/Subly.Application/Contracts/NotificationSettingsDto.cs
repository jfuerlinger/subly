namespace Subly.Application.Contracts;

public sealed record NotificationSettingsDto(int LeadDays, bool EmailEnabled);

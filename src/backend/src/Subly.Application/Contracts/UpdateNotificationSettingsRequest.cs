namespace Subly.Application.Contracts;

public sealed record UpdateNotificationSettingsRequest(int LeadDays, bool EmailEnabled);

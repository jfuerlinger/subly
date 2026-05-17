namespace Subly.Cli.Contracts;

public record DashboardSummaryDto(
    int TotalSubscriptions,
    decimal TotalMonthlyPrice,
    decimal TotalYearlyPrice,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByCategory);

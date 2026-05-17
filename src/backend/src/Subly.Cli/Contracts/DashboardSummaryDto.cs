namespace Subly.Cli.Contracts;

public record DashboardSummaryDto(
    decimal MonthlyTotal,
    decimal YearlyTotal,
    int ActiveSubscriptionsCount,
    decimal UpcomingPaymentsTotal30Days,
    int UpcomingPaymentsCount30Days);

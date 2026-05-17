using Subly.Cli.Contracts;

namespace Subly.Cli.Services;

public static class OutputFormatter
{
    public static void PrintSubscriptionTable(List<SubscriptionDto> subscriptions)
    {
        if (subscriptions.Count == 0)
        {
            Console.WriteLine("No subscriptions found.");
            return;
        }

        const int idWidth = 36;
        const int nameWidth = 20;
        const int vendorWidth = 15;
        const int priceWidth = 12;
        const int statusWidth = 12;

        PrintTableHeader(
            ("ID", idWidth),
            ("Name", nameWidth),
            ("Vendor", vendorWidth),
            ("Price", priceWidth),
            ("Status", statusWidth));

        foreach (var sub in subscriptions)
        {
            PrintTableRow(
                (sub.Id.ToString(), idWidth),
                (sub.Name, nameWidth),
                (sub.Vendor, vendorWidth),
                (sub.Price.ToString("F2"), priceWidth),
                (sub.Status, statusWidth));
        }
    }

    public static void PrintSubscriptionDetail(SubscriptionDto subscription)
    {
        Console.WriteLine("\n=== Subscription Details ===");
        Console.WriteLine($"ID:                {subscription.Id}");
        Console.WriteLine($"Name:              {subscription.Name}");
        Console.WriteLine($"Vendor:            {subscription.Vendor}");
        Console.WriteLine($"Category:          {subscription.Category}");
        Console.WriteLine($"Price:             €{subscription.Price:F2}");
        Console.WriteLine($"Billing Cycle:     {subscription.Cycle}");
        Console.WriteLine($"Next Payment:      {subscription.NextPaymentDate}");
        Console.WriteLine($"Payment Method:    {subscription.PaymentMethod}");
        Console.WriteLine($"Status:            {subscription.Status}");
        Console.WriteLine($"Auto Renew:        {(subscription.AutoRenew ? "Yes" : "No")}");
        Console.WriteLine($"Started:           {subscription.StartedAt}");
        Console.WriteLine($"Cancelled:         {subscription.CancelledAt?.ToString() ?? "Active"}");
        Console.WriteLine();
    }

    public static void PrintCategoryTable(List<CategoryDto> categories)
    {
        if (categories.Count == 0)
        {
            Console.WriteLine("No categories found.");
            return;
        }

        const int idWidth = 36;
        const int nameWidth = 30;

        PrintTableHeader(
            ("ID", idWidth),
            ("Name", nameWidth));

        foreach (var cat in categories)
        {
            PrintTableRow(
                (cat.Id.ToString(), idWidth),
                (cat.Name, nameWidth));
        }
    }

    public static void PrintDashboardSummary(DashboardSummaryDto summary)
    {
        Console.WriteLine("\n=== Dashboard Summary ===");
        Console.WriteLine($"Total Subscriptions:       {summary.TotalSubscriptions}");
        Console.WriteLine($"Total Monthly Price:       €{summary.TotalMonthlyPrice:F2}");
        Console.WriteLine($"Total Yearly Price:        €{summary.TotalYearlyPrice:F2}");

        Console.WriteLine("\nBy Status:");
        foreach (var kvp in summary.ByStatus)
        {
            Console.WriteLine($"  {kvp.Key,-20} {kvp.Value,3} subscriptions");
        }

        Console.WriteLine("\nBy Category:");
        foreach (var kvp in summary.ByCategory)
        {
            Console.WriteLine($"  {kvp.Key,-20} {kvp.Value,3} subscriptions");
        }

        Console.WriteLine();
    }

    private static void PrintTableHeader(params (string, int)[] columns)
    {
        Console.WriteLine();
        foreach (var (header, width) in columns)
        {
            Console.Write(header.PadRight(width));
        }

        Console.WriteLine();
        foreach (var (_, width) in columns)
        {
            Console.Write(new string('-', width));
        }

        Console.WriteLine();
    }

    private static void PrintTableRow(params (string, int)[] columns)
    {
        foreach (var (value, width) in columns)
        {
            var truncated = value.Length > width ? value[..(width - 2)] + ".." : value;
            Console.Write(truncated.PadRight(width));
        }

        Console.WriteLine();
    }
}

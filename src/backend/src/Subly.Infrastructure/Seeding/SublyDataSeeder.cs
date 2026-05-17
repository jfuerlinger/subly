using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;

namespace Subly.Infrastructure.Seeding;

public static class SublyDataSeeder
{
    private static readonly string[] DefaultCategories =
    [
        "streaming",
        "software",
        "insurance",
        "telecom",
        "energy",
        "fitness",
        "news",
        "cloud",
        "membership",
    ];

    public static async Task SeedAsync(SublyDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);

        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        await ForceSeedAsync(dbContext, cancellationToken);
    }

    public static async Task ForceSeedAsync(SublyDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = new[]
        {
            Subscription.Create("Netflix Standard", "Netflix", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(6), "Visa •• 4421", today.AddYears(-4)),
            Subscription.Create("Spotify Family", "Spotify", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(2), "PayPal", today.AddYears(-3)),
            Subscription.Create("ChatGPT Plus", "OpenAI", "software", 22m, BillingCycle.Monthly, today.AddDays(3), "Visa •• 4421", today.AddYears(-1)),
            Subscription.Create("Amazon Prime", "Amazon", "membership", 89.90m, BillingCycle.Yearly, today.AddMonths(2), "Mastercard •• 0044", today.AddYears(-6)),
            Subscription.Create("iCloud+ 200GB", "Apple", "cloud", 2.99m, BillingCycle.Monthly, today.AddDays(10), "Apple Pay", today.AddYears(-5)),
            Subscription.Create("Gym Membership", "Fit Club", "fitness", 29.90m, BillingCycle.Monthly, today.AddDays(12), "SEPA", today.AddYears(-2), today.AddMonths(-1), SubscriptionStatus.Cancelled, autoRenew: false),
        };

        await dbContext.Subscriptions.AddRangeAsync(data, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCategoriesAsync(SublyDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingNames = dbContext.Categories.Select(c => c.Name).ToHashSet();
        var missing = DefaultCategories.Where(name => !existingNames.Contains(name)).ToArray();

        if (missing.Length == 0)
            return;

        var categories = missing.Select(Category.Create).ToArray();
        await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

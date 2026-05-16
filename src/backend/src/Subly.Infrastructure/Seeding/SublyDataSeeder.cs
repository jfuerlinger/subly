using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;

namespace Subly.Infrastructure.Seeding;

public static class SublyDataSeeder
{
    public static async Task SeedAsync(SublyDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = new[]
        {
            Subscription.Create("Netflix Standard", "Netflix", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(6), "Visa •• 4421", today.AddYears(-4)),
            Subscription.Create("Spotify Family", "Spotify", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(2), "PayPal", today.AddYears(-3)),
            Subscription.Create("ChatGPT Plus", "OpenAI", "software", 22m, BillingCycle.Monthly, today.AddDays(3), "Visa •• 4421", today.AddYears(-1)),
            Subscription.Create("Amazon Prime", "Amazon", "membership", 89.90m, BillingCycle.Yearly, today.AddMonths(2), "Mastercard •• 0044", today.AddYears(-6)),
            Subscription.Create("iCloud+ 200GB", "Apple", "cloud", 2.99m, BillingCycle.Monthly, today.AddDays(10), "Apple Pay", today.AddYears(-5)),
        };

        await dbContext.Subscriptions.AddRangeAsync(data, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

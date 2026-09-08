using Subly.Application.Abstractions;
using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;

namespace Subly.Infrastructure.Seeding;

public static class SublyDataSeeder
{
    private const string DemoUserFirstName = "Max";
    private const string DemoUserLastName = "Muster";
    public const string DemoUserEmail = "demo@subly.local";
    public const string DemoUserPassword = "Demo1234!";

    private static readonly string[] DefaultCategories =
    [
        "Streaming",
        "Software",
        "Insurance",
        "Telecom",
        "Energy",
        "Fitness",
        "News",
        "Cloud",
        "Membership",
    ];

    public static async Task SeedAsync(SublyDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);
        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        await ForceSeedAsync(dbContext, passwordHasher, cancellationToken);
    }

    public static async Task ForceSeedAsync(SublyDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);

        var demoUser = await EnsureDemoUserAsync(dbContext, passwordHasher, cancellationToken);
        var categoryIds = dbContext.Categories.ToDictionary(c => c.Name, c => c.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = new[]
        {
            Subscription.Create(demoUser.Id, "Netflix Standard", "Netflix", categoryIds["Streaming"], 17.99m, BillingCycle.Monthly, today.AddDays(6), "Visa •• 4421", today.AddYears(-4)),
            Subscription.Create(demoUser.Id, "Spotify Family", "Spotify", categoryIds["Streaming"], 17.99m, BillingCycle.Monthly, today.AddDays(2), "PayPal", today.AddYears(-3)),
            Subscription.Create(demoUser.Id, "ChatGPT Plus", "OpenAI", categoryIds["Software"], 22m, BillingCycle.Monthly, today.AddDays(3), "Visa •• 4421", today.AddYears(-1)),
            Subscription.Create(demoUser.Id, "Amazon Prime", "Amazon", categoryIds["Membership"], 89.90m, BillingCycle.Yearly, today.AddMonths(2), "Mastercard •• 0044", today.AddYears(-6)),
            Subscription.Create(demoUser.Id, "iCloud+ 200GB", "Apple", categoryIds["Cloud"], 2.99m, BillingCycle.Monthly, today.AddDays(10), "Apple Pay", today.AddYears(-5)),
            Subscription.Create(demoUser.Id, "Gym Membership", "Fit Club", categoryIds["Fitness"], 29.90m, BillingCycle.Monthly, today.AddDays(12), "SEPA", today.AddYears(-2), today.AddMonths(-1), SubscriptionStatus.Cancelled, autoRenew: false),
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

    private static async Task<User> EnsureDemoUserAsync(SublyDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(DemoUserEmail);
        var existingUser = dbContext.Users.SingleOrDefault(x => x.Email == normalizedEmail);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var hashedPassword = passwordHasher.Hash(DemoUserPassword);
        var user = User.Create(
            DemoUserFirstName,
            DemoUserLastName,
            normalizedEmail,
            hashedPassword.HashBase64,
            hashedPassword.SaltBase64,
            hashedPassword.Iterations);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }
}

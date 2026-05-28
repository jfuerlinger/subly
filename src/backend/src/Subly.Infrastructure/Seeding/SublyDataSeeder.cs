using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace Subly.Infrastructure.Seeding;

public static class SublyDataSeeder
{
    private const string DemoUserFirstName = "Max";
    private const string DemoUserLastName = "Muster";
    private const string DemoUserEmail = "demo@subly.local";
    private const string DemoUserPassword = "SublyDemo123!";

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
    }

    public static async Task ForceSeedAsync(SublyDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken);

        var demoUser = await EnsureDemoUserAsync(dbContext, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = new[]
        {
            Subscription.Create(demoUser.Id, "Netflix Standard", "Netflix", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(6), "Visa •• 4421", today.AddYears(-4)),
            Subscription.Create(demoUser.Id, "Spotify Family", "Spotify", "streaming", 17.99m, BillingCycle.Monthly, today.AddDays(2), "PayPal", today.AddYears(-3)),
            Subscription.Create(demoUser.Id, "ChatGPT Plus", "OpenAI", "software", 22m, BillingCycle.Monthly, today.AddDays(3), "Visa •• 4421", today.AddYears(-1)),
            Subscription.Create(demoUser.Id, "Amazon Prime", "Amazon", "membership", 89.90m, BillingCycle.Yearly, today.AddMonths(2), "Mastercard •• 0044", today.AddYears(-6)),
            Subscription.Create(demoUser.Id, "iCloud+ 200GB", "Apple", "cloud", 2.99m, BillingCycle.Monthly, today.AddDays(10), "Apple Pay", today.AddYears(-5)),
            Subscription.Create(demoUser.Id, "Gym Membership", "Fit Club", "fitness", 29.90m, BillingCycle.Monthly, today.AddDays(12), "SEPA", today.AddYears(-2), today.AddMonths(-1), SubscriptionStatus.Cancelled, autoRenew: false),
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

    private static async Task<User> EnsureDemoUserAsync(SublyDbContext dbContext, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(DemoUserEmail);
        var existingUser = dbContext.Users.SingleOrDefault(x => x.Email == normalizedEmail);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var passwordHash = HashPassword(DemoUserPassword);
        var user = User.Create(
            DemoUserFirstName,
            DemoUserLastName,
            normalizedEmail,
            passwordHash.Hash,
            passwordHash.Salt,
            passwordHash.Iterations);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static (string Hash, string Salt, int Iterations) HashPassword(string password)
    {
        const int iterationCount = 100_000;
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterationCount, HashAlgorithmName.SHA256, 32);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), iterationCount);
    }
}

using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class SubscriptionService(
    ISubscriptionRepository repository,
    ICategoryRepository categoryRepository,
    ICurrentUserProvider currentUserProvider,
    IDateProvider dateProvider) : ISubscriptionService
{
    private static readonly IReadOnlyDictionary<string, string> KnownLogoDomains =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["netflix"] = "netflix.com",
            ["spotify"] = "spotify.com",
            ["disney"] = "disneyplus.com",
            ["amazon"] = "amazon.com",
            ["prime"] = "primevideo.com",
            ["openai"] = "openai.com",
            ["chatgpt"] = "openai.com",
            ["github"] = "github.com",
            ["notion"] = "notion.so",
            ["adobe"] = "adobe.com",
            ["microsoft"] = "microsoft.com",
            ["google"] = "google.com",
            ["youtube"] = "youtube.com",
            ["apple"] = "apple.com",
            ["dropbox"] = "dropbox.com",
            ["slack"] = "slack.com",
            ["zoom"] = "zoom.us",
            ["canva"] = "canva.com",
            ["figma"] = "figma.com",
            ["deezer"] = "deezer.com",
        };

    public async Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var subscriptions = await repository.ListAsync(userId, cancellationToken);
        return subscriptions
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto)
            .ToArray();
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var subscription = await repository.GetByIdAsync(id, userId, cancellationToken);
        return subscription is null ? null : ToDto(subscription);
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        await ValidateRequestAsync(request, cancellationToken);

        var initialStatus = request.CancelledAt.HasValue ? SubscriptionStatus.Cancelled : SubscriptionStatus.Active;
        var subscription = Subscription.Create(
            userId,
            request.Name,
            request.Vendor,
            request.Category.ToLowerInvariant(),
            request.Price,
            request.Cycle,
            request.NextPaymentDate,
            request.PaymentMethod,
            request.StartedAt,
            request.CancelledAt,
            initialStatus,
            logoUrl: request.LogoUrl);

        await repository.AddAsync(subscription, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    public async Task<SubscriptionDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var subscription = await repository.GetByIdAsync(id, userId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        await ValidateRequestAsync(
            new CreateSubscriptionRequest(
                request.Name,
                request.Vendor,
                request.Category,
                request.Price,
                request.Cycle,
                request.NextPaymentDate,
                request.PaymentMethod,
                request.StartedAt,
                request.CancelledAt),
            cancellationToken);

        var nextStatus = request.CancelledAt.HasValue
            ? SubscriptionStatus.Cancelled
            : subscription.Status is SubscriptionStatus.Cancelled ? SubscriptionStatus.Active : subscription.Status;

        subscription.UpdateDetails(
            request.Name,
            request.Vendor,
            request.Category.ToLowerInvariant(),
            request.Price,
            request.Cycle,
            request.NextPaymentDate,
            request.PaymentMethod,
            request.StartedAt,
            request.CancelledAt,
            nextStatus,
            request.LogoUrl);

        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(subscription);
    }

    public async Task<SubscriptionDto?> UpdateStatusAsync(Guid id, SubscriptionStatus status, DateOnly? cancelledAt, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var subscription = await repository.GetByIdAsync(id, userId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        DateOnly? effectiveCancelledAt = status is SubscriptionStatus.Cancelled
            ? cancelledAt ?? dateProvider.Today
            : null;
        subscription.UpdateStatus(status, effectiveCancelledAt);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var deleted = await repository.DeleteAsync(id, userId, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var subscriptions = await repository.ListAsync(userId, cancellationToken);
        var activeSubscriptions = subscriptions.Where(x => x.Status is SubscriptionStatus.Active).ToArray();
        var today = dateProvider.Today;
        var end = today.AddDays(30);

        var monthly = activeSubscriptions.Sum(ToMonthlyValue);
        var yearly = activeSubscriptions.Sum(ToYearlyValue);
        var upcoming = activeSubscriptions.Where(x => x.NextPaymentDate >= today && x.NextPaymentDate <= end).ToArray();

        return new DashboardSummaryDto(
            MonthlyTotal: decimal.Round(monthly, 2, MidpointRounding.AwayFromZero),
            YearlyTotal: decimal.Round(yearly, 2, MidpointRounding.AwayFromZero),
            ActiveSubscriptionsCount: activeSubscriptions.Length,
            UpcomingPaymentsTotal30Days: decimal.Round(upcoming.Sum(x => x.Price), 2, MidpointRounding.AwayFromZero),
            UpcomingPaymentsCount30Days: upcoming.Length);
    }

    public IReadOnlyList<LogoSuggestionDto> GetLogoSuggestions(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return [];
        }

        var normalizedName = NormalizeSubscriptionName(name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return [];
        }

        var domains = BuildDomainCandidates(normalizedName);

        return domains
            .SelectMany(BuildLogoSuggestionsForDomain)
            .DistinctBy(x => x.LogoUrl, StringComparer.OrdinalIgnoreCase)
            .Take(9)
            .ToArray();
    }

    private static decimal ToMonthlyValue(Subscription subscription)
    {
        return subscription.Cycle is BillingCycle.Yearly ? subscription.Price / 12m : subscription.Price;
    }

    private static decimal ToYearlyValue(Subscription subscription)
    {
        return subscription.Cycle is BillingCycle.Yearly ? subscription.Price : subscription.Price * 12m;
    }

    private static SubscriptionDto ToDto(Subscription subscription)
    {
        return new SubscriptionDto(
            subscription.Id,
            subscription.Name,
            subscription.Vendor,
            subscription.Category,
            subscription.Price,
            subscription.Cycle,
            subscription.NextPaymentDate,
            subscription.PaymentMethod,
            subscription.Status,
            subscription.AutoRenew,
            subscription.StartedAt,
            subscription.CancelledAt,
            subscription.LogoUrl);
    }

    private static string NormalizeSubscriptionName(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length);
        var previousWasSpace = false;

        foreach (var character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSpace = false;
                continue;
            }

            if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static IEnumerable<string> BuildDomainCandidates(string normalizedName)
    {
        var candidates = new List<string>();
        var compactName = normalizedName.Replace(" ", string.Empty, StringComparison.Ordinal);

        AddKnownMappings(normalizedName, candidates);

        if (!string.IsNullOrWhiteSpace(compactName) && compactName.Length >= 3)
        {
            candidates.Add($"{compactName}.com");
        }

        var words = normalizedName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length > 0)
        {
            AddKnownMappings(words[0], candidates);

            if (words[0].Length >= 3)
            {
                candidates.Add($"{words[0]}.com");
            }
        }

        return candidates
            .Where(IsValidDomain)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<LogoSuggestionDto> BuildLogoSuggestionsForDomain(string domain)
    {
        yield return new LogoSuggestionDto("Clearbit", domain, $"https://logo.clearbit.com/{domain}");
        yield return new LogoSuggestionDto("Google Favicon", domain, $"https://www.google.com/s2/favicons?domain={domain}&sz=128");
        yield return new LogoSuggestionDto("DuckDuckGo Favicon", domain, $"https://icons.duckduckgo.com/ip3/{domain}.ico");
    }

    private static void AddKnownMappings(string key, ICollection<string> candidates)
    {
        if (KnownLogoDomains.TryGetValue(key, out var mappedDomain))
        {
            candidates.Add(mappedDomain);
        }
    }

    private static bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var dotIndex = domain.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == domain.Length - 1)
        {
            return false;
        }

        return domain.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
    }

    private async Task ValidateRequestAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (request.Price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Price), "Price must be greater than zero.");
        }

        if (request.CancelledAt.HasValue && request.CancelledAt.Value < request.StartedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CancelledAt), "Cancelled date cannot be earlier than start date.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request.Category));
        }

        var category = await categoryRepository.GetByNameAsync(request.Category.ToLowerInvariant(), cancellationToken);
        if (category is null)
        {
            throw new ArgumentException($"Unknown category '{request.Category}'.", nameof(request.Category));
        }
    }
}

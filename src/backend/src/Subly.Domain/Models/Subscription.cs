namespace Subly.Domain.Models;

public sealed class Subscription
{
    private Subscription()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Vendor { get; private set; } = string.Empty;

    public Guid CategoryId { get; private set; }

    public decimal Price { get; private set; }

    public BillingCycle Cycle { get; private set; }

    public DateOnly NextPaymentDate { get; private set; }

    public string PaymentMethod { get; private set; } = string.Empty;

    public SubscriptionStatus Status { get; private set; }

    public bool AutoRenew { get; private set; }

    public DateOnly StartedAt { get; private set; }

    public DateOnly? CancelledAt { get; private set; }

    public string? LogoUrl { get; private set; }

    public static Subscription Create(
        Guid userId,
        string name,
        string vendor,
        Guid categoryId,
        decimal price,
        BillingCycle cycle,
        DateOnly nextPaymentDate,
        string paymentMethod,
        DateOnly startedAt,
        DateOnly? cancelledAt = null,
        SubscriptionStatus status = SubscriptionStatus.Active,
        bool autoRenew = true,
        string? logoUrl = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(vendor))
        {
            throw new ArgumentException("Vendor is required.", nameof(vendor));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            throw new ArgumentException("Payment method is required.", nameof(paymentMethod));
        }

        if (cancelledAt.HasValue && cancelledAt.Value < startedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(cancelledAt), "Cancelled date cannot be earlier than start date.");
        }

        if (status is SubscriptionStatus.Cancelled && !cancelledAt.HasValue)
        {
            throw new ArgumentException("Cancelled date is required when status is cancelled.", nameof(cancelledAt));
        }

        if (status is not SubscriptionStatus.Cancelled && cancelledAt.HasValue)
        {
            throw new ArgumentException("Cancelled date can only be set when status is cancelled.", nameof(cancelledAt));
        }

        return new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Vendor = vendor.Trim(),
            CategoryId = categoryId,
            Price = price,
            Cycle = cycle,
            NextPaymentDate = nextPaymentDate,
            PaymentMethod = paymentMethod.Trim(),
            StartedAt = startedAt,
            CancelledAt = cancelledAt,
            Status = status,
            AutoRenew = autoRenew,
            LogoUrl = NormalizeLogoUrl(logoUrl),
        };
    }

    public void UpdateStatus(SubscriptionStatus status, DateOnly? cancelledAt = null)
    {
        if (status is SubscriptionStatus.Cancelled)
        {
            if (!cancelledAt.HasValue)
            {
                throw new ArgumentException("Cancelled date is required when status is cancelled.", nameof(cancelledAt));
            }

            if (cancelledAt.Value < StartedAt)
            {
                throw new ArgumentOutOfRangeException(nameof(cancelledAt), "Cancelled date cannot be earlier than start date.");
            }

            CancelledAt = cancelledAt.Value;
        }
        else
        {
            CancelledAt = null;
        }

        Status = status;
    }

    public void UpdateDetails(
        string name,
        string vendor,
        Guid categoryId,
        decimal price,
        BillingCycle cycle,
        DateOnly nextPaymentDate,
        string paymentMethod,
        DateOnly startedAt,
        DateOnly? cancelledAt,
        SubscriptionStatus status,
        string? logoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(vendor))
        {
            throw new ArgumentException("Vendor is required.", nameof(vendor));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            throw new ArgumentException("Payment method is required.", nameof(paymentMethod));
        }

        if (cancelledAt.HasValue && cancelledAt.Value < startedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(cancelledAt), "Cancelled date cannot be earlier than start date.");
        }

        if (status is SubscriptionStatus.Cancelled && !cancelledAt.HasValue)
        {
            throw new ArgumentException("Cancelled date is required when status is cancelled.", nameof(cancelledAt));
        }

        if (status is not SubscriptionStatus.Cancelled && cancelledAt.HasValue)
        {
            throw new ArgumentException("Cancelled date can only be set when status is cancelled.", nameof(cancelledAt));
        }

        Name = name.Trim();
        Vendor = vendor.Trim();
        CategoryId = categoryId;
        Price = price;
        Cycle = cycle;
        NextPaymentDate = nextPaymentDate;
        PaymentMethod = paymentMethod.Trim();
        StartedAt = startedAt;
        CancelledAt = cancelledAt;
        Status = status;

        if (logoUrl is not null)
        {
            LogoUrl = NormalizeLogoUrl(logoUrl);
        }
    }

    public void ReassignCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        CategoryId = categoryId;
    }

    private static string? NormalizeLogoUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        var trimmedLogoUrl = logoUrl.Trim();

        if (trimmedLogoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            const int maxLogoDataUrlLength = 700_000;
            if (trimmedLogoUrl.Length > maxLogoDataUrlLength)
            {
                throw new ArgumentException("Logo upload is too large.");
            }

            return trimmedLogoUrl;
        }

        if (!Uri.TryCreate(trimmedLogoUrl, UriKind.Absolute, out var parsedUri))
        {
            throw new ArgumentException("Logo URL must be an absolute URL or image data URL.", nameof(logoUrl));
        }

        if (parsedUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Logo URL must use HTTP or HTTPS.", nameof(logoUrl));
        }

        return trimmedLogoUrl;
    }
}

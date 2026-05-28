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

    public string Category { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public BillingCycle Cycle { get; private set; }

    public DateOnly NextPaymentDate { get; private set; }

    public string PaymentMethod { get; private set; } = string.Empty;

    public SubscriptionStatus Status { get; private set; }

    public bool AutoRenew { get; private set; }

    public DateOnly StartedAt { get; private set; }

    public DateOnly? CancelledAt { get; private set; }

    public static Subscription Create(
        Guid userId,
        string name,
        string vendor,
        string category,
        decimal price,
        BillingCycle cycle,
        DateOnly nextPaymentDate,
        string paymentMethod,
        DateOnly startedAt,
        DateOnly? cancelledAt = null,
        SubscriptionStatus status = SubscriptionStatus.Active,
        bool autoRenew = true)
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

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
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
            Category = category.Trim(),
            Price = price,
            Cycle = cycle,
            NextPaymentDate = nextPaymentDate,
            PaymentMethod = paymentMethod.Trim(),
            StartedAt = startedAt,
            CancelledAt = cancelledAt,
            Status = status,
            AutoRenew = autoRenew,
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
        string category,
        decimal price,
        BillingCycle cycle,
        DateOnly nextPaymentDate,
        string paymentMethod,
        DateOnly startedAt,
        DateOnly? cancelledAt,
        SubscriptionStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(vendor))
        {
            throw new ArgumentException("Vendor is required.", nameof(vendor));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
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
        Category = category.Trim();
        Price = price;
        Cycle = cycle;
        NextPaymentDate = nextPaymentDate;
        PaymentMethod = paymentMethod.Trim();
        StartedAt = startedAt;
        CancelledAt = cancelledAt;
        Status = status;
    }
}

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Api.Mcp;

[McpServerToolType]
public sealed class SubscriptionTools(ISubscriptionService subscriptions, ICategoryService categories)
{
    [McpServerTool(Name = "subscription_list", ReadOnly = true, OpenWorld = false)]
    [Description("List all subscriptions owned by the connected Subly account.")]
    public Task<IReadOnlyList<SubscriptionDto>> List(CancellationToken ct) =>
        subscriptions.GetSubscriptionsAsync(ct);

    [McpServerTool(Name = "subscription_get", ReadOnly = true, OpenWorld = false)]
    [Description("Get one subscription by its ID.")]
    public async Task<SubscriptionDto> Get(Guid id, CancellationToken ct) =>
        RequireSubscription(await subscriptions.GetSubscriptionAsync(id, ct));

    [McpServerTool(Name = "category_list", ReadOnly = true, OpenWorld = false)]
    [Description("List available category IDs and names for use when creating or editing subscriptions.")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) =>
        categories.GetCategoriesAsync(ct);

    [McpServerTool(Name = "category_create", Destructive = false, OpenWorld = false)]
    [Description("Create a category shared by all Subly accounts. Use a unique name of up to 40 characters. Returns the ID and name for use with subscriptions.")]
    public Task<CategoryDto> CreateCategory(string name, CancellationToken ct) =>
        Validate(() => categories.CreateCategoryAsync(name, ct));

    [McpServerTool(Name = "dashboard_summary", ReadOnly = true, OpenWorld = false)]
    [Description("Summarize monthly and yearly costs, active subscriptions and payments due in the next 30 days.")]
    public Task<DashboardSummaryDto> Summary(CancellationToken ct) =>
        subscriptions.GetDashboardSummaryAsync(ct);

    [McpServerTool(Name = "subscription_create", Destructive = false, OpenWorld = false)]
    [Description("Create a subscription. Use category_list for categoryId. Supply dates as YYYY-MM-DD.")]
    public Task<SubscriptionDto> Create(CreateSubscriptionRequest subscription, CancellationToken ct) =>
        Validate(() => subscriptions.CreateSubscriptionAsync(subscription, ct));

    [McpServerTool(Name = "subscription_update", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replace all details. Read the current record first to preserve other values. A null cancelledAt reactivates a cancelled subscription. A null logoUrl preserves the logo.")]
    public async Task<SubscriptionDto> Update(Guid id, UpdateSubscriptionRequest subscription, CancellationToken ct) =>
        RequireSubscription(await Validate(() => subscriptions.UpdateSubscriptionAsync(id, subscription, ct)));

    [McpServerTool(Name = "subscription_set_status", Destructive = true, OpenWorld = false)]
    [Description("Set an existing subscription to active, paused or cancelled. Cancellation defaults to today's date.")]
    public async Task<SubscriptionDto> SetStatus(Guid id, SubscriptionStatus status, CancellationToken ct, DateOnly? cancelledAt = null) =>
        RequireSubscription(await Validate(() => subscriptions.UpdateStatusAsync(id, status, cancelledAt, ct)));

    [McpServerTool(Name = "subscription_set_category", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Move a subscription to an existing category, preserving all other details.")]
    public async Task<SubscriptionDto> SetCategory(Guid id, Guid categoryId, CancellationToken ct) =>
        RequireSubscription(await Validate(() => subscriptions.UpdateCategoryAsync(id, categoryId, ct)));

    [McpServerTool(Name = "subscription_suggest_logos", ReadOnly = true, OpenWorld = false)]
    [Description("Suggest candidate icon URLs for a subscription name. Pass a suggested logoUrl to subscription_set_logo.")]
    public IReadOnlyList<LogoSuggestionDto> SuggestLogos(string name) => subscriptions.GetLogoSuggestions(name);

    [McpServerTool(Name = "subscription_set_logo", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Set only the subscription icon. Supply an HTTP(S) image URL, an image data URL (up to 700,000 characters), or an empty string to remove the icon.")]
    public async Task<string> SetLogo(Guid id, string logoUrl, CancellationToken ct) =>
        await Validate(() => subscriptions.SetLogoAsync(id, logoUrl, ct)) ? "Subscription icon updated." : throw NotFound();

    [McpServerTool(Name = "subscription_delete", Destructive = true, OpenWorld = false)]
    [Description("Permanently delete a subscription from Subly. To record a cancellation, use subscription_set_status.")]
    public async Task<string> Delete(Guid id, CancellationToken ct) =>
        await subscriptions.DeleteSubscriptionAsync(id, ct) ? "Subscription deleted." : throw NotFound();

    private static SubscriptionDto RequireSubscription(SubscriptionDto? result) => result ?? throw NotFound();
    private static McpException NotFound() => new("Subscription not found.");

    private static async Task<T> Validate<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message, ex);
        }
    }
}

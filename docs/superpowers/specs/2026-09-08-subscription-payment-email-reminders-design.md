# Subscription Payment Email Reminders — Design (Phase 1: Email)

## Context

Users have no way to be reminded before a subscription payment is charged.
This spec covers **Phase 1**: a configurable, per-user lead time (in days)
before `Subscription.NextPaymentDate`, delivered by email. A second channel
(Signal) is intentionally out of scope and will get its own spec once
`signal-cli` infrastructure exists (see "Out of scope" below).

## Goals

- A user can enable/disable payment reminder emails and configure how many
  days in advance they want to be notified (global setting per user, not
  per subscription).
- Reminders are sent automatically once a day is reached that matches
  `NextPaymentDate - LeadDays`, without manual intervention.
- A given subscription/payment-date combination is never emailed twice,
  even if the check runs more than once, restarts, or overlaps.

## Non-Goals (out of scope for this spec)

- Signal delivery channel (separate future spec; the data model below is
  built so adding it later does not require a schema redesign).
- Per-subscription override of the lead time (global-per-user only, per
  product decision).
- A separate notification email address (always uses the account's login
  email, `User.Email`).
- Automatically advancing `Subscription.NextPaymentDate` after a payment
  occurs. Today nothing in the codebase advances this field automatically
  after a billing cycle passes — it is only changed via `UpdateDetails`.
  This is a pre-existing gap, unrelated to reminders: without it, a
  reminder fires once for the currently configured date and will not fire
  again until the user (or a future feature) moves `NextPaymentDate`
  forward.

## Domain Model (`Subly.Domain.Models`)

### `NotificationChannel` (new enum, `[Flags]`)

```csharp
[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Signal = 2, // reserved for phase 2, not settable via API yet
}
```

### `NotificationSettings` (new aggregate, 1:1 with `User`)

- `Id` (Guid)
- `UserId` (Guid)
- `LeadDays` (int) — validated `0..90`
- `Channels` (`NotificationChannel`) — combination of enabled channels
- `Create(Guid userId, int leadDays, NotificationChannel channels)` — validates `userId != Guid.Empty` and `leadDays` range.
- `Update(int leadDays, NotificationChannel channels)` — same validation, mutates in place.

Mirrors the `Category.Create`/`Rename` style already used in the domain layer.

### `NotificationDeliveryLog` (new entity — idempotency record)

- `Id` (Guid)
- `SubscriptionId` (Guid)
- `Channel` (`NotificationChannel`) — a single concrete value actually sent (e.g. `Email`), never a combined flag set
- `ForPaymentDate` (DateOnly) — the `NextPaymentDate` this reminder was for
- `SentAtUtc` (DateTimeOffset)
- `Create(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate)`

No update/delete operations — write-once record.

## Persistence (`Subly.Infrastructure`)

- `NotificationSettingsEntityConfiguration` and `NotificationDeliveryLogEntityConfiguration`, following the existing `*EntityConfiguration` pattern (see `SubscriptionEntityConfiguration`, `UserEntityConfiguration`).
- `NotificationSettings`: unique index on `UserId`.
- `NotificationDeliveryLog`: **unique index on `(SubscriptionId, Channel, ForPaymentDate)`**. This is the hard guarantee against duplicate sends — enforced at the database level, not only in application logic, so a race between concurrent background-service ticks cannot double-send.
- One new EF Core migration adding both tables.

### Repository interfaces (`Subly.Application.Abstractions`)

```csharp
public interface INotificationSettingsRepository
{
    Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface INotificationDeliveryLogRepository
{
    Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### `ISubscriptionRepository` addition

```csharp
Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default);
```

System-wide (not scoped to a single user), needed because the background
check must scan every user's active subscriptions in one pass. The existing
`ListAsync(Guid userId, ...)` stays unchanged and unused by this feature.

## Application Layer (`Subly.Application.Services`)

### `INotificationSettingsService` / `NotificationSettingsService`

- `GetForCurrentUserAsync(CancellationToken)` → `NotificationSettingsDto`. Resolves the user via `ICurrentUserProvider.GetRequiredUserId()` (same pattern as `SubscriptionService.GetSubscriptionsAsync()`, which takes no explicit `userId` parameter). Returns a default (`LeadDays = 3`, `Channels = None`) when no row exists yet — no row is created until the user explicitly saves settings.
- `UpdateAsync(int leadDays, bool emailEnabled, CancellationToken)` → upserts the current user's `NotificationSettings` (creates if missing, else calls `Update`).

Contracts (`Subly.Application.Contracts`):

```csharp
public sealed record NotificationSettingsDto(int LeadDays, bool EmailEnabled);
public sealed record UpdateNotificationSettingsRequest(int LeadDays, bool EmailEnabled);
```

### `IEmailSender` (new abstraction, `Subly.Application.Abstractions`)

```csharp
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}
```

### `IPaymentReminderService` / `PaymentReminderService`

```csharp
public interface IPaymentReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
```

Logic:

1. `subscriptions = await subscriptionRepository.ListAllActiveAsync(ct)`.
2. Group by `UserId`; for each group, load that user's `NotificationSettings` via `INotificationSettingsRepository`. Skip the group if no settings row exists or `Channels` does not include `Email`.
3. For each subscription in an enabled group, compute `dueDate = subscription.NextPaymentDate.AddDays(-settings.LeadDays)`. If `dueDate != dateProvider.Today`, skip.
4. If `await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate, ct)`, skip (already sent for this exact payment date).
5. Load the owning `User` (for `Email`, `FirstName`) via `IUserRepository`, build the email body (subscription name, vendor, price, payment date), call `emailSender.SendAsync(...)`.
6. On successful send, write a `NotificationDeliveryLog` entry and save. If sending throws, do **not** write the log entry — the next tick will retry it naturally.

Uses `IDateProvider.Today` instead of `DateTime.UtcNow` directly, matching existing convention (and making the service unit-testable with a fake date).

## Infrastructure Implementations

### `SmtpEmailSender : IEmailSender`

- New NuGet dependency: `MailKit`.
- Configuration via a bound `EmailOptions` (`Host`, `Port`, `Username`, `Password`, `FromAddress`, `FromName`, `UseSsl`), read from `appsettings.json` / `appsettings.Development.json` for non-secret defaults and **user-secrets / environment variables for `Username`/`Password`** — no real credentials committed to the repo. A `appsettings.json` entry with empty placeholder values documents the expected shape.

### `PaymentReminderBackgroundService : BackgroundService`

- Lives in `Subly.Api` (registered as a hosted service in `Program.cs`), started automatically with the API — no new Aspire resource needed.
- `PeriodicTimer` firing every **1 hour**. Hourly (not once-daily-at-a-fixed-time) so a restart near a would-be trigger time doesn't cause the day's reminders to be skipped entirely; the delivery-log uniqueness guarantee makes frequent checks safe.
- Each tick: creates a new `IServiceScope` (required because `IPaymentReminderService` and its dependencies, including the `DbContext`, are scoped; `BackgroundService` itself is a singleton), resolves `IPaymentReminderService`, calls `ProcessDueRemindersAsync`.
- Wraps each tick's work in try/catch, logging exceptions via `ILogger`, so one failed tick (e.g. SMTP temporarily down) does not stop future ticks.

## API (`Subly.Api.Controllers`)

`NotificationSettingsController`, `[ApiController]`, `[Route("api/notification-settings")]`, `[Authorize]` (matches `SubscriptionsController`):

- `GET /api/notification-settings` → `200 OK` with `NotificationSettingsDto` (defaults if unset).
- `PUT /api/notification-settings` → body `UpdateNotificationSettingsRequest`, `200 OK` with the updated `NotificationSettingsDto`. `400 ValidationProblem` on invalid `LeadDays` (mirrors the `ArgumentException` → `ValidationProblem` pattern in `CategoriesController`).

## Frontend

- `src/frontend/src/app/api/notificationSettingsApi.ts` — `fetchNotificationSettings()`, `updateNotificationSettings(payload)`, following the shape of `categoriesApi.ts`.
- Settings state: extend `profileStore.ts` with `leadDays` / `emailEnabled` state and a `saveNotificationSettings` action, consistent with how the store already holds `firstName`/`lastName`.
- UI: a new card in `ProfileSettingsView.vue`, alongside the existing "Persönliche Informationen" and theme cards — a checkbox "E-Mail-Erinnerungen aktivieren" and a number input "Tage vorher" (bounded 0–90 in the UI to match domain validation), with the existing save-feedback pattern (`✓ Gespeichert`).

## Error Handling

- Domain validation (`LeadDays` out of range) throws `ArgumentException`/`ArgumentOutOfRangeException`, surfaced as `400` by the controller, same as existing services.
- SMTP failures inside `PaymentReminderService.ProcessDueRemindersAsync` propagate up to the background service's per-tick try/catch and are logged; the affected subscription is retried on the next tick since no delivery-log entry was written.
- No user-facing error surface is needed for send failures — this is a fire-and-forget background process, not a user-triggered action.

## Testing

- **Unit tests** for `PaymentReminderService`: fake `IDateProvider`, `IEmailSender`, and the two repositories. Cover: exact date match triggers a send, non-matching dates are skipped, an existing delivery-log entry prevents a resend, a disabled/missing `NotificationSettings` row causes no send, a failed `IEmailSender.SendAsync` does not create a delivery-log entry.
- **Domain unit tests** for `NotificationSettings.Create`/`Update` validation boundaries (`LeadDays` 0, 90, -1, 91).
- **Integration tests** for `NotificationSettingsController` (`Subly.Api.Tests`), following the existing pattern of creating fresh test data per test rather than mutating shared seeded data (per project convention).

## Migration / Rollout Notes

- New tables only — no changes to existing tables, so this ships without touching current data.
- `NotificationSettings` rows are created lazily on first save; existing users simply see the default (email disabled, 3 days) until they opt in.
- Real SMTP credentials must be provided via user-secrets/environment configuration before the background service can actually deliver mail in any environment; until configured, `ProcessDueRemindersAsync` will still run and log but `SmtpEmailSender.SendAsync` will fail (caught and logged per the Error Handling section above) rather than crash the app.

## Out of Scope — Phase 2 (Signal)

Tracked separately, to be brainstormed once `signal-cli` is actually running
(a dedicated phone number registered, exposed via a REST API, most likely
as its own container). Expected shape based on this design: add
`NotificationChannel.Signal` to the enabled-channels checkbox set, a new
`ISignalSender` abstraction parallel to `IEmailSender`, and a
`PhoneNumber` field somewhere reachable from `PaymentReminderService`
(most likely on `User`, given the "no per-subscription override" decision
for lead time also implies a per-user Signal target). No schema changes to
`NotificationDeliveryLog` are anticipated — `NotificationChannel.Signal` is
already a valid enum value it can log against.

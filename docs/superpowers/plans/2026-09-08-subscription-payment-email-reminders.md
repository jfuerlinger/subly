# Subscription Payment Email Reminders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a user enable a daily email reminder that fires N days before each of their active subscriptions is next charged, with no duplicate sends.

**Architecture:** A new `NotificationSettings` aggregate (per-user lead time + enabled channels) and a `NotificationDeliveryLog` (idempotency record) sit alongside the existing `Category`/`Subscription` domain models. An hourly in-process `BackgroundService` in `Subly.Api` scans all active subscriptions system-wide, matches them against each owner's settings, and sends email via a new `IEmailSender`/MailKit adapter. A new `NotificationSettingsController` lets the frontend read/write the per-user setting from a new card on `ProfileSettingsView.vue`.

**Tech Stack:** .NET 10, EF Core 10 / Npgsql, MailKit (SMTP), ASP.NET Core `BackgroundService`, Vue 3 + Pinia, Vitest, xUnit + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-09-08-subscription-payment-email-reminders-design.md`

## Global Constraints

- Lead time (`LeadDays`) is global per user, not per subscription; valid range is `0..90` inclusive.
- Only `NotificationChannel.Email` is settable via the API this phase; `NotificationChannel.Signal` exists in the enum but is unreachable from the API/UI until a future spec.
- Reminder emails always go to `User.Email` — no separate notification address field.
- A given `(SubscriptionId, Channel, ForPaymentDate)` combination may only ever send once — enforced by a unique DB index, not just application logic.
- The background check runs hourly via `PeriodicTimer`, not once daily at a fixed time.
- SMTP credentials are read from configuration (user-secrets/environment in real use); `appsettings.json` only holds empty placeholders — never commit real credentials.
- New NuGet packages (`MailKit`, `Microsoft.Extensions.Options.ConfigurationExtensions`) are added unpinned (`dotnet add package <name>`) so NuGet resolves the current stable release, since no version is already pinned elsewhere in the repo for them.
- Backend tests never mutate shared seeded data (e.g. default categories) — every test creates its own fresh user/subscription/category data, per existing project convention.

---

## File Structure

**Backend — new files:**
- `src/Subly.Domain/Models/NotificationChannel.cs`
- `src/Subly.Domain/Models/NotificationSettings.cs`
- `src/Subly.Domain/Models/NotificationDeliveryLog.cs`
- `src/Subly.Application/Abstractions/INotificationSettingsRepository.cs`
- `src/Subly.Application/Abstractions/INotificationDeliveryLogRepository.cs`
- `src/Subly.Application/Abstractions/IEmailSender.cs`
- `src/Subly.Application/Contracts/NotificationSettingsDto.cs`
- `src/Subly.Application/Contracts/UpdateNotificationSettingsRequest.cs`
- `src/Subly.Application/Services/INotificationSettingsService.cs`
- `src/Subly.Application/Services/NotificationSettingsService.cs`
- `src/Subly.Application/Services/IPaymentReminderService.cs`
- `src/Subly.Application/Services/PaymentReminderService.cs`
- `src/Subly.Infrastructure/Persistence/Configurations/NotificationSettingsEntityConfiguration.cs`
- `src/Subly.Infrastructure/Persistence/Configurations/NotificationDeliveryLogEntityConfiguration.cs`
- `src/Subly.Infrastructure/Persistence/Repositories/EfNotificationSettingsRepository.cs`
- `src/Subly.Infrastructure/Persistence/Repositories/EfNotificationDeliveryLogRepository.cs`
- `src/Subly.Infrastructure/Services/EmailOptions.cs`
- `src/Subly.Infrastructure/Services/SmtpEmailSender.cs`
- `src/Subly.Infrastructure/Persistence/Migrations/<timestamp>_AddNotificationSettingsAndDeliveryLog.cs` (generated)
- `src/Subly.Api/BackgroundServices/PaymentReminderBackgroundService.cs`
- `src/Subly.Api/Controllers/NotificationSettingsController.cs`

**Backend — modified files:**
- `src/Subly.Application/Abstractions/ISubscriptionRepository.cs` (add `ListAllActiveAsync`)
- `src/Subly.Infrastructure/Persistence/Repositories/EfSubscriptionRepository.cs` (implement it)
- `src/Subly.Infrastructure/Persistence/SublyDbContext.cs` (two new `DbSet`s)
- `src/Subly.Infrastructure/DependencyInjection.cs` (register new repos, email sender, options)
- `src/Subly.Infrastructure/Subly.Infrastructure.csproj` (MailKit, Options.ConfigurationExtensions)
- `src/Subly.Api/Program.cs` (register new services + hosted service)
- `src/Subly.Api/appsettings.json` (Email section placeholders)

**Backend — new/modified tests:**
- `tests/Subly.Application.Tests/SubscriptionServiceTests.cs` (fake gets `ListAllActiveAsync`)
- `tests/Subly.Application.Tests/NotificationSettingsTests.cs` (new — domain validation)
- `tests/Subly.Application.Tests/NotificationSettingsServiceTests.cs` (new)
- `tests/Subly.Application.Tests/PaymentReminderServiceTests.cs` (new)
- `tests/Subly.Api.Tests/NotificationSettingsEndpointsTests.cs` (new)
- `tests/Subly.Api.Tests/EmailOptionsBindingTests.cs` (new)
- `tests/Subly.Api.Tests/PaymentReminderBackgroundServiceTests.cs` (new)
- `tests/Subly.Api.Tests/PaymentReminderIntegrationTests.cs` (new)
- `tests/Subly.Api.Tests/CustomWebApplicationFactory.cs` (modified — swap in a `TestEmailSender`)

**Frontend — new files:**
- `src/frontend/src/app/api/notificationSettingsApi.ts`
- `src/frontend/src/app/stores/notificationSettingsStore.ts`
- `src/frontend/src/tests/notificationSettingsApi.test.ts`
- `src/frontend/src/tests/notificationSettingsStore.test.ts`

**Frontend — modified files:**
- `src/frontend/src/views/ProfileSettingsView.vue`

---

### Task 1: Domain models — `NotificationChannel`, `NotificationSettings`, `NotificationDeliveryLog`

**Files:**
- Create: `src/backend/src/Subly.Domain/Models/NotificationChannel.cs`
- Create: `src/backend/src/Subly.Domain/Models/NotificationSettings.cs`
- Create: `src/backend/src/Subly.Domain/Models/NotificationDeliveryLog.cs`
- Test: `src/backend/tests/Subly.Application.Tests/NotificationSettingsTests.cs`

**Interfaces:**
- Produces: `NotificationChannel` (`[Flags]` enum: `None = 0`, `Email = 1`, `Signal = 2`), `NotificationSettings.Create(Guid userId, int leadDays, NotificationChannel channels)`, `NotificationSettings.Update(int leadDays, NotificationChannel channels)`, properties `Id`, `UserId`, `LeadDays`, `Channels`. `NotificationDeliveryLog.Create(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate)`, properties `Id`, `SubscriptionId`, `Channel`, `ForPaymentDate`, `SentAtUtc`.

- [ ] **Step 1: Write the failing tests**

```csharp
// src/backend/tests/Subly.Application.Tests/NotificationSettingsTests.cs
using FluentAssertions;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class NotificationSettingsTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_ShouldSucceed_WithValidLeadDays()
    {
        var settings = NotificationSettings.Create(UserId, 3, NotificationChannel.Email);

        settings.UserId.Should().Be(UserId);
        settings.LeadDays.Should().Be(3);
        settings.Channels.Should().Be(NotificationChannel.Email);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public void Create_ShouldThrow_WhenLeadDaysOutOfRange(int leadDays)
    {
        var act = () => NotificationSettings.Create(UserId, leadDays, NotificationChannel.Email);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        var act = () => NotificationSettings.Create(Guid.Empty, 3, NotificationChannel.Email);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldChangeLeadDaysAndChannels()
    {
        var settings = NotificationSettings.Create(UserId, 3, NotificationChannel.None);

        settings.Update(7, NotificationChannel.Email);

        settings.LeadDays.Should().Be(7);
        settings.Channels.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public void NotificationDeliveryLog_Create_ShouldSetFields()
    {
        var subscriptionId = Guid.NewGuid();
        var forPaymentDate = new DateOnly(2026, 9, 15);

        var log = NotificationDeliveryLog.Create(subscriptionId, NotificationChannel.Email, forPaymentDate);

        log.SubscriptionId.Should().Be(subscriptionId);
        log.Channel.Should().Be(NotificationChannel.Email);
        log.ForPaymentDate.Should().Be(forPaymentDate);
    }

    [Fact]
    public void NotificationDeliveryLog_Create_ShouldThrow_WhenChannelIsNone()
    {
        var act = () => NotificationDeliveryLog.Create(Guid.NewGuid(), NotificationChannel.None, new DateOnly(2026, 9, 15));

        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter NotificationSettingsTests`
Expected: FAIL — `NotificationSettings`/`NotificationChannel`/`NotificationDeliveryLog` do not exist yet (compile error).

- [ ] **Step 3: Write the domain types**

```csharp
// src/backend/src/Subly.Domain/Models/NotificationChannel.cs
namespace Subly.Domain.Models;

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Signal = 2,
}
```

```csharp
// src/backend/src/Subly.Domain/Models/NotificationSettings.cs
namespace Subly.Domain.Models;

public sealed class NotificationSettings
{
    private NotificationSettings() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int LeadDays { get; private set; }
    public NotificationChannel Channels { get; private set; }

    public static NotificationSettings Create(Guid userId, int leadDays, NotificationChannel channels)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ValidateLeadDays(leadDays);

        return new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeadDays = leadDays,
            Channels = channels,
        };
    }

    public void Update(int leadDays, NotificationChannel channels)
    {
        ValidateLeadDays(leadDays);
        LeadDays = leadDays;
        Channels = channels;
    }

    private static void ValidateLeadDays(int leadDays)
    {
        if (leadDays is < 0 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(leadDays), "Lead days must be between 0 and 90.");
        }
    }
}
```

```csharp
// src/backend/src/Subly.Domain/Models/NotificationDeliveryLog.cs
namespace Subly.Domain.Models;

public sealed class NotificationDeliveryLog
{
    private NotificationDeliveryLog() { }

    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DateOnly ForPaymentDate { get; private set; }
    public DateTimeOffset SentAtUtc { get; private set; }

    public static NotificationDeliveryLog Create(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        }

        if (channel is NotificationChannel.None)
        {
            throw new ArgumentException("A concrete channel is required.", nameof(channel));
        }

        return new NotificationDeliveryLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Channel = channel,
            ForPaymentDate = forPaymentDate,
            SentAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter NotificationSettingsTests`
Expected: PASS (7 tests)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Domain/Models/NotificationChannel.cs src/backend/src/Subly.Domain/Models/NotificationSettings.cs src/backend/src/Subly.Domain/Models/NotificationDeliveryLog.cs src/backend/tests/Subly.Application.Tests/NotificationSettingsTests.cs
git commit -m "feat: add NotificationSettings and NotificationDeliveryLog domain models"
```

---

### Task 2: `ISubscriptionRepository.ListAllActiveAsync`

**Files:**
- Modify: `src/backend/src/Subly.Application/Abstractions/ISubscriptionRepository.cs`
- Modify: `src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfSubscriptionRepository.cs`
- Modify: `src/backend/tests/Subly.Application.Tests/SubscriptionServiceTests.cs` (its private `InMemorySubscriptionRepository` must implement the new method or the file stops compiling)

**Interfaces:**
- Produces: `Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default)` — every active subscription, across all users (unlike the existing `ListAsync(Guid userId, ...)`, which is scoped to one user). Consumed by `PaymentReminderService` in Task 8.

- [ ] **Step 1: Add the method to the interface**

```csharp
// src/backend/src/Subly.Application/Abstractions/ISubscriptionRepository.cs
// Add inside the interface, after ListAsync(Guid userId, ...):
    Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Run the build to confirm it now fails**

Run: `cd src/backend && dotnet build`
Expected: FAIL — `EfSubscriptionRepository` and the test fake `InMemorySubscriptionRepository` no longer satisfy `ISubscriptionRepository`.

- [ ] **Step 3: Implement it in `EfSubscriptionRepository` and update the test fake**

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfSubscriptionRepository.cs
// Add as a new method on EfSubscriptionRepository:
    public async Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Subscriptions
            .AsNoTracking()
            .Where(x => x.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
    }
```

```csharp
// src/backend/tests/Subly.Application.Tests/SubscriptionServiceTests.cs
// Add as a new method on the private InMemorySubscriptionRepository class:
        public Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => x.Status == SubscriptionStatus.Active).ToList());
        }
```

- [ ] **Step 4: Run the full backend test suite to confirm nothing broke**

Run: `cd src/backend && dotnet test`
Expected: PASS — all existing tests still pass. (`ListAllActiveAsync`'s own behavior is exercised end-to-end by Task 8's and Task 10's tests, since it has no dedicated caller yet.)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Application/Abstractions/ISubscriptionRepository.cs src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfSubscriptionRepository.cs src/backend/tests/Subly.Application.Tests/SubscriptionServiceTests.cs
git commit -m "feat: add ListAllActiveAsync to ISubscriptionRepository"
```

---

### Task 3: Persistence — EF configurations, `DbSet`s, migration

**Files:**
- Create: `src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationSettingsEntityConfiguration.cs`
- Create: `src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationDeliveryLogEntityConfiguration.cs`
- Modify: `src/backend/src/Subly.Infrastructure/Persistence/SublyDbContext.cs`
- Generated: `src/backend/src/Subly.Infrastructure/Persistence/Migrations/<timestamp>_AddNotificationSettingsAndDeliveryLog.cs` (+ `.Designer.cs`, updated `SublyDbContextModelSnapshot.cs`)

**Interfaces:**
- Consumes: `NotificationSettings`, `NotificationDeliveryLog`, `NotificationChannel` (Task 1), `User`, `Subscription` (existing).
- Produces: `SublyDbContext.NotificationSettings` and `SublyDbContext.NotificationDeliveryLogs` `DbSet`s, consumed by the repositories in Task 4.

- [ ] **Step 1: Write the entity configurations**

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationSettingsEntityConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class NotificationSettingsEntityConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("NotificationSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LeadDays).IsRequired();
        builder.Property(x => x.Channels).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationDeliveryLogEntityConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class NotificationDeliveryLogEntityConfiguration : IEntityTypeConfiguration<NotificationDeliveryLog>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryLog> builder)
    {
        builder.ToTable("NotificationDeliveryLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionId).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ForPaymentDate).IsRequired();
        builder.Property(x => x.SentAtUtc).IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.Channel, x.ForPaymentDate }).IsUnique();

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Add the `DbSet`s**

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/SublyDbContext.cs
// Add alongside the existing DbSets:
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<NotificationDeliveryLog> NotificationDeliveryLogs => Set<NotificationDeliveryLog>();
```

- [ ] **Step 3: Generate the migration**

Run (from `src/backend`; install the tool first if missing with `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add AddNotificationSettingsAndDeliveryLog \
  --project src/Subly.Infrastructure/Subly.Infrastructure.csproj \
  --startup-project src/Subly.Api/Subly.Api.csproj
```

Expected: three files created/updated under `src/Subly.Infrastructure/Persistence/Migrations/` — the new migration, its `.Designer.cs`, and `SublyDbContextModelSnapshot.cs`. Open the generated migration and confirm it creates `NotificationSettings` and `NotificationDeliveryLogs` tables with the unique indexes described above, and nothing else changed.

- [ ] **Step 4: Build to confirm everything compiles**

Run: `cd src/backend && dotnet build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationSettingsEntityConfiguration.cs src/backend/src/Subly.Infrastructure/Persistence/Configurations/NotificationDeliveryLogEntityConfiguration.cs src/backend/src/Subly.Infrastructure/Persistence/SublyDbContext.cs src/backend/src/Subly.Infrastructure/Persistence/Migrations/
git commit -m "feat: add NotificationSettings and NotificationDeliveryLogs tables"
```

---

### Task 4: EF repository implementations + DI registration

**Files:**
- Create: `src/backend/src/Subly.Application/Abstractions/INotificationSettingsRepository.cs`
- Create: `src/backend/src/Subly.Application/Abstractions/INotificationDeliveryLogRepository.cs`
- Create: `src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationSettingsRepository.cs`
- Create: `src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationDeliveryLogRepository.cs`
- Modify: `src/backend/src/Subly.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces: `INotificationSettingsRepository` (`GetByUserIdAsync`, `AddAsync`, `SaveChangesAsync`), `INotificationDeliveryLogRepository` (`ExistsAsync`, `AddAsync`, `SaveChangesAsync`) — consumed by `NotificationSettingsService` (Task 5) and `PaymentReminderService` (Task 8).

- [ ] **Step 1: Define the repository interfaces**

```csharp
// src/backend/src/Subly.Application/Abstractions/INotificationSettingsRepository.cs
using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface INotificationSettingsRepository
{
    Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// src/backend/src/Subly.Application/Abstractions/INotificationDeliveryLogRepository.cs
using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface INotificationDeliveryLogRepository
{
    Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Run the build to confirm it fails**

Run: `cd src/backend && dotnet build`
Expected: FAIL — nothing implements the two new interfaces yet, but that alone won't fail the build (interfaces with no implementers compile fine). Instead, skip to implementing directly since there is no failing-test checkpoint for a pure interface addition; proceed to Step 3.

- [ ] **Step 3: Implement the EF repositories**

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationSettingsRepository.cs
using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Repositories;

internal sealed class EfNotificationSettingsRepository(SublyDbContext dbContext) : INotificationSettingsRepository
{
    public Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationSettings.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
    {
        await dbContext.NotificationSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
// src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationDeliveryLogRepository.cs
using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Repositories;

internal sealed class EfNotificationDeliveryLogRepository(SublyDbContext dbContext) : INotificationDeliveryLogRepository
{
    public Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationDeliveryLogs.AnyAsync(
            x => x.SubscriptionId == subscriptionId && x.Channel == channel && x.ForPaymentDate == forPaymentDate,
            cancellationToken);
    }

    public async Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.NotificationDeliveryLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Register them in DI**

```csharp
// src/backend/src/Subly.Infrastructure/DependencyInjection.cs
// Add alongside the existing services.AddScoped<ICategoryRepository, ...> line:
        services.AddScoped<INotificationSettingsRepository, EfNotificationSettingsRepository>();
        services.AddScoped<INotificationDeliveryLogRepository, EfNotificationDeliveryLogRepository>();
```

Run: `cd src/backend && dotnet build`
Expected: PASS. (Behavior is verified through `NotificationSettingsService` tests in Task 5 and the end-to-end test in Task 10 — matches the existing convention that `EfCategoryRepository`/`EfUserRepository` have no repository-level tests of their own.)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Application/Abstractions/INotificationSettingsRepository.cs src/backend/src/Subly.Application/Abstractions/INotificationDeliveryLogRepository.cs src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationSettingsRepository.cs src/backend/src/Subly.Infrastructure/Persistence/Repositories/EfNotificationDeliveryLogRepository.cs src/backend/src/Subly.Infrastructure/DependencyInjection.cs
git commit -m "feat: add notification settings and delivery log repositories"
```

---

### Task 5: `NotificationSettingsService`

**Files:**
- Create: `src/backend/src/Subly.Application/Contracts/NotificationSettingsDto.cs`
- Create: `src/backend/src/Subly.Application/Contracts/UpdateNotificationSettingsRequest.cs`
- Create: `src/backend/src/Subly.Application/Services/INotificationSettingsService.cs`
- Create: `src/backend/src/Subly.Application/Services/NotificationSettingsService.cs`
- Test: `src/backend/tests/Subly.Application.Tests/NotificationSettingsServiceTests.cs`

**Interfaces:**
- Consumes: `INotificationSettingsRepository` (Task 4), `ICurrentUserProvider` (existing), `NotificationSettings.Create`/`Update` (Task 1).
- Produces: `INotificationSettingsService.GetForCurrentUserAsync(CancellationToken)` → `NotificationSettingsDto(int LeadDays, bool EmailEnabled)`; `UpdateAsync(UpdateNotificationSettingsRequest, CancellationToken)` → `NotificationSettingsDto`. Consumed by `NotificationSettingsController` (Task 6).

- [ ] **Step 1: Write the failing tests**

```csharp
// src/backend/tests/Subly.Application.Tests/NotificationSettingsServiceTests.cs
using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class NotificationSettingsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task GetForCurrentUserAsync_ShouldReturnDefaults_WhenNoSettingsExist()
    {
        var service = new NotificationSettingsService(
            new InMemoryNotificationSettingsRepository(),
            new FixedCurrentUserProvider(CurrentUserId));

        var result = await service.GetForCurrentUserAsync();

        result.LeadDays.Should().Be(3);
        result.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldCreateSettings_WhenNoneExistYet()
    {
        var repository = new InMemoryNotificationSettingsRepository();
        var service = new NotificationSettingsService(repository, new FixedCurrentUserProvider(CurrentUserId));

        var result = await service.UpdateAsync(new UpdateNotificationSettingsRequest(5, true));

        result.LeadDays.Should().Be(5);
        result.EmailEnabled.Should().BeTrue();
        var stored = await repository.GetByUserIdAsync(CurrentUserId);
        stored.Should().NotBeNull();
        stored!.Channels.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSettings()
    {
        var repository = new InMemoryNotificationSettingsRepository();
        var service = new NotificationSettingsService(repository, new FixedCurrentUserProvider(CurrentUserId));
        await service.UpdateAsync(new UpdateNotificationSettingsRequest(5, true));

        var result = await service.UpdateAsync(new UpdateNotificationSettingsRequest(10, false));

        result.LeadDays.Should().Be(10);
        result.EmailEnabled.Should().BeFalse();
        var stored = await repository.GetByUserIdAsync(CurrentUserId);
        stored!.LeadDays.Should().Be(10);
        stored.Channels.Should().Be(NotificationChannel.None);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenLeadDaysOutOfRange()
    {
        var service = new NotificationSettingsService(
            new InMemoryNotificationSettingsRepository(),
            new FixedCurrentUserProvider(CurrentUserId));

        var act = () => service.UpdateAsync(new UpdateNotificationSettingsRequest(-1, true));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private sealed class FixedCurrentUserProvider(Guid userId) : ICurrentUserProvider
    {
        public Guid GetRequiredUserId() => userId;
    }

    private sealed class InMemoryNotificationSettingsRepository : INotificationSettingsRepository
    {
        private readonly List<NotificationSettings> _items = [];

        public Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.UserId == userId));
        }

        public Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
        {
            _items.Add(settings);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter NotificationSettingsServiceTests`
Expected: FAIL — `NotificationSettingsService`, `NotificationSettingsDto`, `UpdateNotificationSettingsRequest` don't exist yet.

- [ ] **Step 3: Write the contracts and service**

```csharp
// src/backend/src/Subly.Application/Contracts/NotificationSettingsDto.cs
namespace Subly.Application.Contracts;

public sealed record NotificationSettingsDto(int LeadDays, bool EmailEnabled);
```

```csharp
// src/backend/src/Subly.Application/Contracts/UpdateNotificationSettingsRequest.cs
namespace Subly.Application.Contracts;

public sealed record UpdateNotificationSettingsRequest(int LeadDays, bool EmailEnabled);
```

```csharp
// src/backend/src/Subly.Application/Services/INotificationSettingsService.cs
using Subly.Application.Contracts;

namespace Subly.Application.Services;

public interface INotificationSettingsService
{
    Task<NotificationSettingsDto> GetForCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<NotificationSettingsDto> UpdateAsync(UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default);
}
```

```csharp
// src/backend/src/Subly.Application/Services/NotificationSettingsService.cs
using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class NotificationSettingsService(
    INotificationSettingsRepository repository,
    ICurrentUserProvider currentUserProvider) : INotificationSettingsService
{
    private const int DefaultLeadDays = 3;

    public async Task<NotificationSettingsDto> GetForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var settings = await repository.GetByUserIdAsync(userId, cancellationToken);

        return settings is null
            ? new NotificationSettingsDto(DefaultLeadDays, false)
            : ToDto(settings);
    }

    public async Task<NotificationSettingsDto> UpdateAsync(UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.GetRequiredUserId();
        var channels = request.EmailEnabled ? NotificationChannel.Email : NotificationChannel.None;
        var settings = await repository.GetByUserIdAsync(userId, cancellationToken);

        if (settings is null)
        {
            settings = NotificationSettings.Create(userId, request.LeadDays, channels);
            await repository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.Update(request.LeadDays, channels);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private static NotificationSettingsDto ToDto(NotificationSettings settings)
    {
        return new NotificationSettingsDto(settings.LeadDays, settings.Channels.HasFlag(NotificationChannel.Email));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter NotificationSettingsServiceTests`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Application/Contracts/NotificationSettingsDto.cs src/backend/src/Subly.Application/Contracts/UpdateNotificationSettingsRequest.cs src/backend/src/Subly.Application/Services/INotificationSettingsService.cs src/backend/src/Subly.Application/Services/NotificationSettingsService.cs src/backend/tests/Subly.Application.Tests/NotificationSettingsServiceTests.cs
git commit -m "feat: add NotificationSettingsService"
```

---

### Task 6: `NotificationSettingsController` + integration tests

**Files:**
- Create: `src/backend/src/Subly.Api/Controllers/NotificationSettingsController.cs`
- Modify: `src/backend/src/Subly.Api/Program.cs`
- Test: `src/backend/tests/Subly.Api.Tests/NotificationSettingsEndpointsTests.cs`

**Interfaces:**
- Consumes: `INotificationSettingsService` (Task 5).
- Produces: `GET /api/notification-settings`, `PUT /api/notification-settings`.

- [ ] **Step 1: Write the failing tests**

```csharp
// src/backend/tests/Subly.Api.Tests/NotificationSettingsEndpointsTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Subly.Application.Contracts;

namespace Subly.Api.Tests;

public sealed class NotificationSettingsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetNotificationSettings_ShouldReturnDefaults_ForNewUser()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var result = await client.GetFromJsonAsync<NotificationSettingsDto>("/api/notification-settings");

        result.Should().NotBeNull();
        result!.LeadDays.Should().Be(3);
        result.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotificationSettings_ShouldPersistAndReturnUpdatedSettings()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var response = await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(7, true));
        var body = await response.Content.ReadFromJsonAsync<NotificationSettingsDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.LeadDays.Should().Be(7);
        body.EmailEnabled.Should().BeTrue();

        var refetched = await client.GetFromJsonAsync<NotificationSettingsDto>("/api/notification-settings");
        refetched!.LeadDays.Should().Be(7);
        refetched.EmailEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotificationSettings_ShouldReturnBadRequest_WhenLeadDaysOutOfRange()
    {
        var client = await CreateAuthenticatedClientAsync($"notify-{Guid.NewGuid():N}@example.com");

        var response = await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(91, true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNotificationSettings_ShouldRequireAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notification-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: email,
            Password: "Secure123!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter NotificationSettingsEndpointsTests`
Expected: FAIL — 404s, since the route doesn't exist yet.

- [ ] **Step 3: Write the controller and register the service**

```csharp
// src/backend/src/Subly.Api/Controllers/NotificationSettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/notification-settings")]
[Authorize]
public sealed class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetForCurrentUserAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationSettingsDto>> Update([FromBody] UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
```

```csharp
// src/backend/src/Subly.Api/Program.cs
// Add alongside the existing builder.Services.AddScoped<ICategoryService, CategoryService>(); line:
builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter NotificationSettingsEndpointsTests`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Api/Controllers/NotificationSettingsController.cs src/backend/src/Subly.Api/Program.cs src/backend/tests/Subly.Api.Tests/NotificationSettingsEndpointsTests.cs
git commit -m "feat: add notification settings API endpoints"
```

---

### Task 7: `IEmailSender` + `SmtpEmailSender` (MailKit)

**Files:**
- Create: `src/backend/src/Subly.Application/Abstractions/IEmailSender.cs`
- Create: `src/backend/src/Subly.Infrastructure/Services/EmailOptions.cs`
- Create: `src/backend/src/Subly.Infrastructure/Services/SmtpEmailSender.cs`
- Modify: `src/backend/src/Subly.Infrastructure/Subly.Infrastructure.csproj` (MailKit, Options.ConfigurationExtensions)
- Modify: `src/backend/src/Subly.Infrastructure/DependencyInjection.cs`
- Modify: `src/backend/src/Subly.Api/appsettings.json`
- Test: `src/backend/tests/Subly.Api.Tests/EmailOptionsBindingTests.cs`

**Interfaces:**
- Produces: `IEmailSender.SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken)`, consumed by `PaymentReminderService` (Task 8). `EmailOptions` (`Host`, `Port`, `Username`, `Password`, `FromAddress`, `FromName`, `UseSsl`), bound from the `"Email"` configuration section.

- [ ] **Step 1: Write the failing test**

```csharp
// src/backend/tests/Subly.Api.Tests/EmailOptionsBindingTests.cs
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subly.Infrastructure.Services;

namespace Subly.Api.Tests;

public sealed class EmailOptionsBindingTests
{
    [Fact]
    public void EmailOptions_ShouldBindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = "smtp.example.com",
                ["Email:Port"] = "2525",
                ["Email:Username"] = "user@example.com",
                ["Email:Password"] = "secret",
                ["Email:FromAddress"] = "noreply@subly.local",
                ["Email:FromName"] = "Subly Reminders",
                ["Email:UseSsl"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<EmailOptions>>().Value;

        options.Host.Should().Be("smtp.example.com");
        options.Port.Should().Be(2525);
        options.Username.Should().Be("user@example.com");
        options.FromAddress.Should().Be("noreply@subly.local");
        options.FromName.Should().Be("Subly Reminders");
        options.UseSsl.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter EmailOptionsBindingTests`
Expected: FAIL — `Subly.Infrastructure.Services.EmailOptions` doesn't exist yet.

- [ ] **Step 3: Add the packages, options, sender, and DI/config wiring**

```bash
cd src/backend/src/Subly.Infrastructure
dotnet add package MailKit
dotnet add package Microsoft.Extensions.Options.ConfigurationExtensions
```

```csharp
// src/backend/src/Subly.Application/Abstractions/IEmailSender.cs
namespace Subly.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}
```

```csharp
// src/backend/src/Subly.Infrastructure/Services/EmailOptions.cs
namespace Subly.Infrastructure.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Subly";
    public bool UseSsl { get; set; } = true;
}
```

```csharp
// src/backend/src/Subly.Infrastructure/Services/SmtpEmailSender.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Subly.Application.Abstractions;

namespace Subly.Infrastructure.Services;

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = bodyHtml };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
```

```csharp
// src/backend/src/Subly.Infrastructure/DependencyInjection.cs
// Add inside AddInfrastructure, alongside the other registrations:
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
```

```json
// src/backend/src/Subly.Api/appsettings.json
// Add a new top-level section (placeholders only — real credentials go in user-secrets/environment config):
  "Email": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromAddress": "",
    "FromName": "Subly",
    "UseSsl": true
  },
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter EmailOptionsBindingTests`
Expected: PASS. (`SmtpEmailSender` itself is not unit-tested against a real SMTP server — same convention as `PasswordHasher`/`JwtTokenService`, which are only exercised through fakes at the consumer level; `IEmailSender` is faked in Task 8's and Task 10's tests.)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Application/Abstractions/IEmailSender.cs src/backend/src/Subly.Infrastructure/Services/EmailOptions.cs src/backend/src/Subly.Infrastructure/Services/SmtpEmailSender.cs src/backend/src/Subly.Infrastructure/Subly.Infrastructure.csproj src/backend/src/Subly.Infrastructure/DependencyInjection.cs src/backend/src/Subly.Api/appsettings.json src/backend/tests/Subly.Api.Tests/EmailOptionsBindingTests.cs
git commit -m "feat: add SMTP email sender"
```

---

### Task 8: `PaymentReminderService`

**Files:**
- Create: `src/backend/src/Subly.Application/Services/IPaymentReminderService.cs`
- Create: `src/backend/src/Subly.Application/Services/PaymentReminderService.cs`
- Test: `src/backend/tests/Subly.Application.Tests/PaymentReminderServiceTests.cs`

**Interfaces:**
- Consumes: `ISubscriptionRepository.ListAllActiveAsync` (Task 2), `IUserRepository.GetByIdAsync` (existing), `INotificationSettingsRepository`, `INotificationDeliveryLogRepository` (Task 4), `IEmailSender` (Task 7), `IDateProvider` (existing).
- Produces: `IPaymentReminderService.ProcessDueRemindersAsync(CancellationToken)`, consumed by `PaymentReminderBackgroundService` (Task 9) and the integration test (Task 10).

- [ ] **Step 1: Write the failing tests**

```csharp
// src/backend/tests/Subly.Application.Tests/PaymentReminderServiceTests.cs
using FluentAssertions;
using Subly.Application.Abstractions;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Application.Tests;

public sealed class PaymentReminderServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSendEmail_WhenDueDateMatchesToday()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var subscriptionRepository = new InMemorySubscriptionRepository([subscription]);
        var userRepository = new InMemoryUserRepository([user]);
        var settingsRepository = new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]);
        var deliveryLogRepository = new InMemoryNotificationDeliveryLogRepository();
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            subscriptionRepository, userRepository, settingsRepository, deliveryLogRepository, emailSender, new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
        emailSender.SentMessages[0].ToEmail.Should().Be("user@example.com");
        (await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenDueDateDoesNotMatchToday()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(10), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenEmailChannelDisabled()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.None)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSkip_WhenNoSettingsExistForUser()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository(),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldNotSendTwice_ForSamePaymentDate()
    {
        var today = new DateOnly(2026, 9, 8);
        var user = CreateUser("user@example.com");
        var subscription = Subscription.Create(user.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([subscription]),
            new InMemoryUserRepository([user]),
            new InMemoryNotificationSettingsRepository([NotificationSettings.Create(user.Id, 3, NotificationChannel.Email)]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();
        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldOnlyNotifyMatchingUser_AcrossMultipleUsers()
    {
        var today = new DateOnly(2026, 9, 8);
        var dueUser = CreateUser("due@example.com");
        var otherUser = CreateUser("other@example.com");
        var dueSubscription = Subscription.Create(dueUser.Id, "Netflix", "Netflix", CategoryId, 17.99m, BillingCycle.Monthly, today.AddDays(3), "Visa", today.AddYears(-1));
        var otherSubscription = Subscription.Create(otherUser.Id, "Spotify", "Spotify", CategoryId, 9.99m, BillingCycle.Monthly, today.AddDays(20), "Visa", today.AddYears(-1));
        var emailSender = new FakeEmailSender();
        var service = new PaymentReminderService(
            new InMemorySubscriptionRepository([dueSubscription, otherSubscription]),
            new InMemoryUserRepository([dueUser, otherUser]),
            new InMemoryNotificationSettingsRepository([
                NotificationSettings.Create(dueUser.Id, 3, NotificationChannel.Email),
                NotificationSettings.Create(otherUser.Id, 3, NotificationChannel.Email),
            ]),
            new InMemoryNotificationDeliveryLogRepository(),
            emailSender,
            new FixedDateProvider(today));

        await service.ProcessDueRemindersAsync();

        emailSender.SentMessages.Should().ContainSingle();
        emailSender.SentMessages[0].ToEmail.Should().Be("due@example.com");
    }

    private static User CreateUser(string email)
    {
        return User.Create("Max", "Muster", email, "hash", "salt", 1);
    }

    private sealed class FixedDateProvider(DateOnly today) : IDateProvider
    {
        public DateOnly Today => today;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string ToEmail, string Subject, string BodyHtml)> SentMessages { get; } = [];

        public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((toEmail, subject, bodyHtml));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySubscriptionRepository(IEnumerable<Subscription>? seed = null) : ISubscriptionRepository
    {
        private readonly List<Subscription> _items = seed?.ToList() ?? [];

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            _items.Add(subscription);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.RemoveAll(x => x.Id == id && x.UserId == userId) > 0);
        }

        public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }

        public Task<Subscription?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Id == id && x.UserId == userId));
        }

        public Task<IReadOnlyList<Subscription>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => x.UserId == userId).ToList());
        }

        public Task<IReadOnlyList<Subscription>> ListAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(_items.Where(x => x.Status == SubscriptionStatus.Active).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserRepository(IEnumerable<User> seed) : IUserRepository
    {
        private readonly List<User> _items = seed.ToList();

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Email == email));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _items.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationSettingsRepository(IEnumerable<NotificationSettings>? seed = null) : INotificationSettingsRepository
    {
        private readonly List<NotificationSettings> _items = seed?.ToList() ?? [];

        public Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.SingleOrDefault(x => x.UserId == userId));
        }

        public Task AddAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
        {
            _items.Add(settings);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationDeliveryLogRepository : INotificationDeliveryLogRepository
    {
        private readonly List<NotificationDeliveryLog> _items = [];

        public Task<bool> ExistsAsync(Guid subscriptionId, NotificationChannel channel, DateOnly forPaymentDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.Any(x => x.SubscriptionId == subscriptionId && x.Channel == channel && x.ForPaymentDate == forPaymentDate));
        }

        public Task AddAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default)
        {
            _items.Add(log);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter PaymentReminderServiceTests`
Expected: FAIL — `PaymentReminderService`/`IPaymentReminderService` don't exist yet.

- [ ] **Step 3: Implement the service**

```csharp
// src/backend/src/Subly.Application/Services/IPaymentReminderService.cs
namespace Subly.Application.Services;

public interface IPaymentReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// src/backend/src/Subly.Application/Services/PaymentReminderService.cs
using Subly.Application.Abstractions;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class PaymentReminderService(
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    INotificationSettingsRepository notificationSettingsRepository,
    INotificationDeliveryLogRepository deliveryLogRepository,
    IEmailSender emailSender,
    IDateProvider dateProvider) : IPaymentReminderService
{
    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await subscriptionRepository.ListAllActiveAsync(cancellationToken);
        var today = dateProvider.Today;

        foreach (var group in subscriptions.GroupBy(x => x.UserId))
        {
            var settings = await notificationSettingsRepository.GetByUserIdAsync(group.Key, cancellationToken);
            if (settings is null || !settings.Channels.HasFlag(NotificationChannel.Email))
            {
                continue;
            }

            foreach (var subscription in group)
            {
                var dueDate = subscription.NextPaymentDate.AddDays(-settings.LeadDays);
                if (dueDate != today)
                {
                    continue;
                }

                var alreadySent = await deliveryLogRepository.ExistsAsync(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate, cancellationToken);
                if (alreadySent)
                {
                    continue;
                }

                var user = await userRepository.GetByIdAsync(group.Key, cancellationToken);
                if (user is null)
                {
                    continue;
                }

                await emailSender.SendAsync(
                    user.Email,
                    $"Erinnerung: {subscription.Name} wird bald abgebucht",
                    BuildReminderHtml(subscription, settings.LeadDays),
                    cancellationToken);

                var log = NotificationDeliveryLog.Create(subscription.Id, NotificationChannel.Email, subscription.NextPaymentDate);
                await deliveryLogRepository.AddAsync(log, cancellationToken);
                await deliveryLogRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static string BuildReminderHtml(Subscription subscription, int leadDays)
    {
        return $"""
            <p>Hallo,</p>
            <p>dein Abo <strong>{subscription.Name}</strong> ({subscription.Vendor}) wird in {leadDays} Tag(en), am {subscription.NextPaymentDate:dd.MM.yyyy}, mit {subscription.Price:0.00} € abgebucht.</p>
            """;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/backend && dotnet test tests/Subly.Application.Tests --filter PaymentReminderServiceTests`
Expected: PASS (6 tests)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Application/Services/IPaymentReminderService.cs src/backend/src/Subly.Application/Services/PaymentReminderService.cs src/backend/tests/Subly.Application.Tests/PaymentReminderServiceTests.cs
git commit -m "feat: add PaymentReminderService"
```

---

### Task 9: `PaymentReminderBackgroundService`

**Files:**
- Create: `src/backend/src/Subly.Api/BackgroundServices/PaymentReminderBackgroundService.cs`
- Modify: `src/backend/src/Subly.Api/Program.cs`
- Test: `src/backend/tests/Subly.Api.Tests/PaymentReminderBackgroundServiceTests.cs`

**Interfaces:**
- Consumes: `IPaymentReminderService` (Task 8), resolved per-tick from a fresh `IServiceScope`.
- Produces: `PaymentReminderBackgroundService.RunOnceAsync(CancellationToken)` (public, directly testable without waiting on the internal `PeriodicTimer`), registered as a hosted service.

- [ ] **Step 1: Write the failing tests**

```csharp
// src/backend/tests/Subly.Api.Tests/PaymentReminderBackgroundServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Subly.Api.BackgroundServices;
using Subly.Application.Services;

namespace Subly.Api.Tests;

public sealed class PaymentReminderBackgroundServiceTests
{
    [Fact]
    public async Task RunOnceAsync_ShouldInvokePaymentReminderService()
    {
        var fakeReminderService = new FakePaymentReminderService();
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentReminderService>(fakeReminderService);
        var provider = services.BuildServiceProvider();
        var backgroundService = new PaymentReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PaymentReminderBackgroundService>.Instance);

        await backgroundService.RunOnceAsync(CancellationToken.None);

        fakeReminderService.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task RunOnceAsync_ShouldNotThrow_WhenReminderServiceFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentReminderService>(new ThrowingPaymentReminderService());
        var provider = services.BuildServiceProvider();
        var backgroundService = new PaymentReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PaymentReminderBackgroundService>.Instance);

        var act = () => backgroundService.RunOnceAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private sealed class FakePaymentReminderService : IPaymentReminderService
    {
        public int CallCount { get; private set; }

        public Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPaymentReminderService : IPaymentReminderService
    {
        public Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter PaymentReminderBackgroundServiceTests`
Expected: FAIL — `PaymentReminderBackgroundService` doesn't exist yet.

- [ ] **Step 3: Implement the background service and register it**

```csharp
// src/backend/src/Subly.Api/BackgroundServices/PaymentReminderBackgroundService.cs
using Subly.Application.Services;

namespace Subly.Api.BackgroundServices;

public sealed class PaymentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IPaymentReminderService>();
            await reminderService.ProcessDueRemindersAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process due payment reminders.");
        }
    }
}
```

```csharp
// src/backend/src/Subly.Api/Program.cs
// Add near the other builder.Services.AddScoped<...> registrations:
builder.Services.AddScoped<IPaymentReminderService, PaymentReminderService>();
builder.Services.AddHostedService<PaymentReminderBackgroundService>();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter PaymentReminderBackgroundServiceTests`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Subly.Api/BackgroundServices/PaymentReminderBackgroundService.cs src/backend/src/Subly.Api/Program.cs src/backend/tests/Subly.Api.Tests/PaymentReminderBackgroundServiceTests.cs
git commit -m "feat: run payment reminder checks on an hourly background service"
```

---

### Task 10: End-to-end integration test

**Files:**
- Modify: `src/backend/tests/Subly.Api.Tests/CustomWebApplicationFactory.cs` (swap in a capturing `TestEmailSender`)
- Test: `src/backend/tests/Subly.Api.Tests/PaymentReminderIntegrationTests.cs`

**Interfaces:**
- Produces: `CustomWebApplicationFactory.EmailSender` (a `TestEmailSender`, public, so tests can assert on captured sends) — usable by any future test in `Subly.Api.Tests` that needs to check outgoing mail without a real SMTP server.

- [ ] **Step 1: Write the failing test**

```csharp
// src/backend/tests/Subly.Api.Tests/PaymentReminderIntegrationTests.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Api.Tests;

public sealed class PaymentReminderIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProcessDueRemindersAsync_ShouldSendEmailOnce_ForSubscriptionDueAtConfiguredLeadTime()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var email = $"reminder-{uniqueSuffix}@example.com";
        var client = await CreateAuthenticatedClientAsync(email);
        var categories = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        (await client.PutAsJsonAsync("/api/notification-settings", new UpdateNotificationSettingsRequest(3, true))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/subscriptions", new CreateSubscriptionRequest(
            Name: "Reminder Test",
            Vendor: "Subly",
            CategoryId: categoryId,
            Price: 9.99m,
            Cycle: BillingCycle.Monthly,
            NextPaymentDate: today.AddDays(3),
            PaymentMethod: "Visa",
            StartedAt: today.AddMonths(-1),
            CancelledAt: null))).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var reminderService = scope.ServiceProvider.GetRequiredService<IPaymentReminderService>();
            await reminderService.ProcessDueRemindersAsync();
            await reminderService.ProcessDueRemindersAsync();
        }

        factory.EmailSender.SentMessages.Should().ContainSingle(m => m.ToEmail == email);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest(
            FirstName: "Max",
            LastName: "Muster",
            Email: email,
            Password: "Secure123!"));
        registerResponse.EnsureSuccessStatusCode();

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        return client;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter PaymentReminderIntegrationTests`
Expected: FAIL — `CustomWebApplicationFactory` has no `EmailSender` property yet, and without a fake, the real `SmtpEmailSender` would try (and fail) to connect to a real SMTP server.

- [ ] **Step 3: Add the `TestEmailSender` to the factory**

```csharp
// src/backend/tests/Subly.Api.Tests/CustomWebApplicationFactory.cs
// Add these usings at the top:
using Subly.Application.Abstractions;

// Add as a new public class in this file, after CustomWebApplicationFactory:
public sealed class TestEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string BodyHtml)> SentMessages { get; } = [];

    public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((toEmail, subject, bodyHtml));
        return Task.CompletedTask;
    }
}

// Inside CustomWebApplicationFactory, add a property:
    public TestEmailSender EmailSender { get; } = new();

// Inside ConfigureWebHost's services.ConfigureServices(services => { ... }) block, alongside the existing
// services.Replace(ServiceDescriptor.Scoped<SublyDbContext>(...)) call, add:
            services.Replace(ServiceDescriptor.Singleton<IEmailSender>(EmailSender));
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/backend && dotnet test tests/Subly.Api.Tests --filter PaymentReminderIntegrationTests`
Expected: PASS. This exercises the real `EfSubscriptionRepository.ListAllActiveAsync`, `EfNotificationSettingsRepository`, `EfNotificationDeliveryLogRepository`, and the migration's schema together for the first time.

- [ ] **Step 5: Run the entire backend test suite**

Run: `cd src/backend && dotnet test`
Expected: PASS — every test across `Subly.Application.Tests` and `Subly.Api.Tests`.

- [ ] **Step 6: Commit**

```bash
git add src/backend/tests/Subly.Api.Tests/CustomWebApplicationFactory.cs src/backend/tests/Subly.Api.Tests/PaymentReminderIntegrationTests.cs
git commit -m "test: add end-to-end payment reminder integration test"
```

---

### Task 11: Frontend — `notificationSettingsApi.ts`

**Files:**
- Create: `src/frontend/src/app/api/notificationSettingsApi.ts`
- Test: `src/frontend/src/tests/notificationSettingsApi.test.ts`

**Interfaces:**
- Produces: `NotificationSettingsDto { leadDays: number; emailEnabled: boolean }`, `fetchNotificationSettings(): Promise<NotificationSettingsDto>`, `updateNotificationSettings(leadDays: number, emailEnabled: boolean): Promise<NotificationSettingsDto>`. Consumed by the store in Task 12.

- [ ] **Step 1: Write the failing test**

```typescript
// src/frontend/src/tests/notificationSettingsApi.test.ts
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import {
  fetchNotificationSettings,
  updateNotificationSettings,
  type NotificationSettingsDto,
} from '../app/api/notificationSettingsApi'

describe('notificationSettingsApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('fetches notification settings', async () => {
    const payload: NotificationSettingsDto = { leadDays: 3, emailEnabled: false }
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: payload } as AxiosResponse<NotificationSettingsDto>)

    const result = await fetchNotificationSettings()

    expect(result).toEqual(payload)
    expect(apiClient.get).toHaveBeenCalledWith('/notification-settings')
  })

  it('updates notification settings', async () => {
    const updated: NotificationSettingsDto = { leadDays: 7, emailEnabled: true }
    vi.spyOn(apiClient, 'put').mockResolvedValue({ data: updated } as AxiosResponse<NotificationSettingsDto>)

    const result = await updateNotificationSettings(7, true)

    expect(result).toEqual(updated)
    expect(apiClient.put).toHaveBeenCalledWith('/notification-settings', { leadDays: 7, emailEnabled: true })
  })
})
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/tests/notificationSettingsApi.test.ts`
Expected: FAIL — module `../app/api/notificationSettingsApi` doesn't exist yet.

- [ ] **Step 3: Write the API client module**

```typescript
// src/frontend/src/app/api/notificationSettingsApi.ts
import { apiClient } from './client'

export interface NotificationSettingsDto {
  leadDays: number
  emailEnabled: boolean
}

export async function fetchNotificationSettings(): Promise<NotificationSettingsDto> {
  const response = await apiClient.get<NotificationSettingsDto>('/notification-settings')
  return response.data
}

export async function updateNotificationSettings(
  leadDays: number,
  emailEnabled: boolean,
): Promise<NotificationSettingsDto> {
  const response = await apiClient.put<NotificationSettingsDto>('/notification-settings', {
    leadDays,
    emailEnabled,
  })
  return response.data
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/tests/notificationSettingsApi.test.ts`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/app/api/notificationSettingsApi.ts src/frontend/src/tests/notificationSettingsApi.test.ts
git commit -m "feat: add notification settings API client"
```

---

### Task 12: Frontend — `notificationSettingsStore.ts`

**Files:**
- Create: `src/frontend/src/app/stores/notificationSettingsStore.ts`
- Test: `src/frontend/src/tests/notificationSettingsStore.test.ts`

**Interfaces:**
- Consumes: `fetchNotificationSettings`, `updateNotificationSettings` (Task 11).
- Produces: `useNotificationSettingsStore()` with reactive `leadDays: number`, `emailEnabled: boolean`, `loading: boolean`, `error: string | null`, and actions `initialize(): Promise<void>`, `save(leadDays: number, emailEnabled: boolean): Promise<void>`. Consumed by `ProfileSettingsView.vue` in Task 13.

- [ ] **Step 1: Write the failing test**

```typescript
// src/frontend/src/tests/notificationSettingsStore.test.ts
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useNotificationSettingsStore } from '../app/stores/notificationSettingsStore'
import * as notificationSettingsApi from '../app/api/notificationSettingsApi'

describe('useNotificationSettingsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
  })

  it('initializes with defaults', () => {
    const store = useNotificationSettingsStore()

    expect(store.leadDays).toBe(3)
    expect(store.emailEnabled).toBe(false)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('loads settings on initialize', async () => {
    vi.spyOn(notificationSettingsApi, 'fetchNotificationSettings').mockResolvedValue({ leadDays: 5, emailEnabled: true })
    const store = useNotificationSettingsStore()

    await store.initialize()

    expect(store.leadDays).toBe(5)
    expect(store.emailEnabled).toBe(true)
    expect(store.error).toBeNull()
  })

  it('sets error when initialize fails', async () => {
    vi.spyOn(notificationSettingsApi, 'fetchNotificationSettings').mockRejectedValue(new Error('Network error'))
    const store = useNotificationSettingsStore()

    await store.initialize()

    expect(store.error).toBeTruthy()
  })

  it('saves settings and updates state', async () => {
    const update = vi.spyOn(notificationSettingsApi, 'updateNotificationSettings').mockResolvedValue({ leadDays: 10, emailEnabled: true })
    const store = useNotificationSettingsStore()

    await store.save(10, true)

    expect(update).toHaveBeenCalledWith(10, true)
    expect(store.leadDays).toBe(10)
    expect(store.emailEnabled).toBe(true)
  })
})
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/tests/notificationSettingsStore.test.ts`
Expected: FAIL — module `../app/stores/notificationSettingsStore` doesn't exist yet.

- [ ] **Step 3: Write the store**

```typescript
// src/frontend/src/app/stores/notificationSettingsStore.ts
import { ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchNotificationSettings, updateNotificationSettings } from '../api/notificationSettingsApi'

export const useNotificationSettingsStore = defineStore('notificationSettings', () => {
  const leadDays = ref(3)
  const emailEnabled = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function initialize(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const settings = await fetchNotificationSettings()
      leadDays.value = settings.leadDays
      emailEnabled.value = settings.emailEnabled
    } catch {
      error.value = 'Die Benachrichtigungseinstellungen konnten nicht geladen werden.'
    } finally {
      loading.value = false
    }
  }

  async function save(newLeadDays: number, newEmailEnabled: boolean): Promise<void> {
    const settings = await updateNotificationSettings(newLeadDays, newEmailEnabled)
    leadDays.value = settings.leadDays
    emailEnabled.value = settings.emailEnabled
  }

  return { leadDays, emailEnabled, loading, error, initialize, save }
})
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/tests/notificationSettingsStore.test.ts`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/app/stores/notificationSettingsStore.ts src/frontend/src/tests/notificationSettingsStore.test.ts
git commit -m "feat: add notification settings store"
```

---

### Task 13: Frontend — `ProfileSettingsView.vue` UI

**Files:**
- Modify: `src/frontend/src/views/ProfileSettingsView.vue`

**Interfaces:**
- Consumes: `useNotificationSettingsStore()` (Task 12).

- [ ] **Step 1: Add the script logic**

```vue
<!-- src/frontend/src/views/ProfileSettingsView.vue -->
<!-- Add to the existing <script setup> imports: -->
import { onMounted, ref } from 'vue'
import { useNotificationSettingsStore } from '../app/stores/notificationSettingsStore'

<!-- Add alongside the existing `const themeStore = ...` / `const profileStore = ...` lines: -->
const notificationSettingsStore = useNotificationSettingsStore()
const leadDaysInput = ref(3)
const emailEnabledInput = ref(false)
const notificationSettingsSaved = ref(false)

onMounted(async () => {
  await notificationSettingsStore.initialize()
  leadDaysInput.value = notificationSettingsStore.leadDays
  emailEnabledInput.value = notificationSettingsStore.emailEnabled
})

async function saveNotificationSettings() {
  await notificationSettingsStore.save(leadDaysInput.value, emailEnabledInput.value)
  notificationSettingsSaved.value = true
  setTimeout(() => { notificationSettingsSaved.value = false }, 2000)
}
```

- [ ] **Step 2: Add the card to the template**

```vue
<!-- Add as a new "card profile-section" between the "Persönliche Informationen" card and the "Darstellung" card: -->
    <div class="card profile-section">
      <h3>Zahlungserinnerungen</h3>
      <p class="muted" style="margin-bottom: 1rem;">Erhalte eine E-Mail, bevor ein Abo abgebucht wird.</p>

      <form class="name-form" @submit.prevent="saveNotificationSettings">
        <label class="checkbox-field">
          <input v-model="emailEnabledInput" type="checkbox" />
          E-Mail-Erinnerungen aktivieren
        </label>
        <div class="form-field" style="max-width: 160px;">
          <label for="lead-days">Tage vorher</label>
          <input id="lead-days" v-model.number="leadDaysInput" type="number" min="0" max="90" />
        </div>
        <div class="name-actions">
          <button type="submit" class="btn-primary">Speichern</button>
          <span v-if="notificationSettingsSaved" class="save-feedback">✓ Gespeichert</span>
        </div>
      </form>
    </div>
```

- [ ] **Step 3: Add the checkbox row style**

```css
/* Add to the existing <style scoped> block: */
.checkbox-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9375rem;
  cursor: pointer;
}
```

- [ ] **Step 4: Manually verify in the browser**

Run: `cd src/backend && aspire run` (or the project's usual dev startup), then open `/profile` (or wherever `ProfileSettingsView` is routed) in the browser.
Expected: a new "Zahlungserinnerungen" card appears between "Persönliche Informationen" and "Darstellung", showing the checkbox unchecked and "3" as the default lead time for a fresh user. Check the box, change the number, click "Speichern", see "✓ Gespeichert", then reload the page and confirm both values persisted (round-tripped through the backend, not just local state).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/views/ProfileSettingsView.vue
git commit -m "feat: add payment reminder settings to profile settings page"
```

# Subly CLI Usage Guide

## Overview

**Subly CLI** is a command-line interface for managing subscriptions in the Subly subscription management system. The CLI provides full access to all API endpoints, allowing you to create, list, update, and manage your subscriptions directly from your terminal.

**Core Principle:** The CLI is a thin client that calls the Subly API directly. No business logic exists in the CLI—all operations are performed via API endpoints.

---

## Installation & Setup

### Prerequisites
- .NET 10.0 or later
- Running Subly API server (default: `http://localhost:5000`)

### Building the CLI

```bash
cd src/backend
dotnet build src/Subly.Cli/Subly.Cli.csproj -c Release
```

### Running the CLI

```bash
# From the build output directory
./Subly.Cli [verb] [options] [arguments]

# Or via dotnet
dotnet run --project src/Subly.Cli/Subly.Cli.csproj -- [verb] [options] [arguments]
```

### API URL Configuration

By default, the CLI connects to `http://localhost:5000`. To use a different API server:

```bash
--api-url https://api.example.com
# or
-u https://api.example.com
```

All commands support the `--api-url` option to override the default API endpoint.

---

## Subscription Commands

### List Subscriptions

List all active subscriptions.

```bash
dotnet run --project src/Subly.Cli -- subscription-list
dotnet run --project src/Subly.Cli -- subscription-list --api-url http://api.example.com
```

**Output:** Formatted table with ID, Name, Vendor, Price, and Status.

---

### Get Subscription Details

Retrieve detailed information about a specific subscription.

```bash
dotnet run --project src/Subly.Cli -- subscription-get <subscription-id>
```

**Example:**
```bash
dotnet run --project src/Subly.Cli -- subscription-get 550e8400-e29b-41d4-a716-446655440000
```

**Output:** Full subscription details including all fields.

---

### Create Subscription

Create a new subscription with required details.

```bash
dotnet run --project src/Subly.Cli -- subscription-create \
  --name "Netflix Premium" \
  --vendor "Netflix Inc." \
  --category "streaming" \
  --price "15.99" \
  --cycle "monthly" \
  --next-payment "2026-06-17" \
  --payment-method "credit_card" \
  --started "2024-01-01"
```

**Options:**
- `--name` (required): Subscription name
- `--vendor` (required): Vendor or service provider name
- `--category` (required): Category (e.g., streaming, software, fitness, insurance, etc.)
- `--price` (required): Monthly/cycle price in euros
- `--cycle` (required): Billing cycle (monthly, yearly, quarterly)
- `--next-payment` (required): Next payment date (format: yyyy-MM-dd)
- `--payment-method` (required): Payment method (e.g., credit_card, paypal, bank_transfer)
- `--started` (required): Subscription start date (format: yyyy-MM-dd)
- `--cancelled` (optional): Cancellation date (format: yyyy-MM-dd)

---

### Update Subscription Status

Change the status of an active subscription.

```bash
dotnet run --project src/Subly.Cli -- subscription-update-status <subscription-id> \
  --status "paused"
```

**Valid Statuses:**
- `active`: Subscription is active
- `paused`: Subscription is paused
- `cancelled`: Subscription is cancelled

**Options:**
- `--status` (required): New subscription status
- `--cancelled` (optional): Cancellation date for cancelled subscriptions (format: yyyy-MM-dd)

---

### Delete Subscription

Remove a subscription from the system. By default, this prompts for confirmation.

```bash
dotnet run --project src/Subly.Cli -- subscription-delete <subscription-id>
```

**Example (with confirmation):**
```bash
dotnet run --project src/Subly.Cli -- subscription-delete 550e8400-e29b-41d4-a716-446655440000
# Are you sure you want to delete subscription 550e8400-e29b-41d4-a716-446655440000? (yes/no):
```

**Options:**
- `--yes` or `-y`: Skip confirmation and delete immediately

---

## Dashboard Commands

### Get Dashboard Summary

Retrieve subscription dashboard statistics including active subscription count, monthly/yearly totals, and upcoming payment metrics.

```bash
dotnet run --project src/Subly.Cli -- dashboard-summary
```

**Output:**
- Active subscription count
- Total monthly cost
- Total yearly cost
- Upcoming payments in next 30 days (total and count)

---

## Category Commands

### List Categories

List all available subscription categories.

```bash
dotnet run --project src/Subly.Cli -- category-list
```

**Output:** Formatted table with ID and Name.

---

### Create Category

Create a new custom subscription category.

```bash
dotnet run --project src/Subly.Cli -- category-create --name "Education"
```

**Options:**
- `--name` or `-n` (required): Category name

---

## Admin Commands

### Fully Reset Database

Deletes the entire database, reapplies all migrations, and reseeds the default data.

```bash
dotnet run --project src/Subly.Cli -- database-reset
```

**Options:**
- `--yes` or `-y`: Skip confirmation
- `--api-url` or `-u`: Base API URL (default: `http://localhost:5000`)

**Example (without confirmation):**
```bash
dotnet run --project src/Subly.Cli -- database-reset --yes
```

### Rename Category

Rename an existing subscription category.

```bash
dotnet run --project src/Subly.Cli -- category-rename --id <category-id> --name "New Name"
```

**Options:**
- `--id` or `-i` (required): Category ID (GUID)
- `--name` or `-n` (required): New category name

---

## Examples

### Complete Workflow

```bash
# 1. List all categories to see available options
dotnet run --project src/Subly.Cli -- category-list

# 2. Create a new subscription
dotnet run --project src/Subly.Cli -- subscription-create \
  --name "GitHub Copilot" \
  --vendor "GitHub" \
  --category "software" \
  --price "19" \
  --cycle "monthly" \
  --next-payment "2026-06-17" \
  --payment-method "credit_card" \
  --started "2024-06-17"

# 3. View the dashboard summary
dotnet run --project src/Subly.Cli -- dashboard-summary

# 4. List all subscriptions
dotnet run --project src/Subly.Cli -- subscription-list

# 5. Update subscription status
dotnet run --project src/Subly.Cli -- subscription-update-status <subscription-id> --status "paused"

# 6. Delete a subscription
dotnet run --project src/Subly.Cli -- subscription-delete <subscription-id> --yes
```

---

## Error Handling

The CLI provides clear error messages for common issues:

- **Invalid ID format**: "Invalid subscription ID format"
- **Invalid date format**: "Invalid date format (use yyyy-MM-dd)"
- **API connection error**: Shows the endpoint and error details
- **Not found**: "Subscription with ID {id} not found"

Exit codes:
- `0`: Command successful
- `1`: Command failed (error message printed to stderr)

---

## Architecture Notes

### API Integration
- All CLI commands make HTTP requests to the Subly API
- The CLI maps API request and response payloads for its commands, with CLI contract models kept aligned to the API where practical
- No caching or local storage; all data is live from the API

### Available Endpoints Mapped to Verbs

| Endpoint | CLI Verb |
|----------|----------|
| GET /api/subscriptions | subscription-list |
| GET /api/subscriptions/{id} | subscription-get |
| POST /api/subscriptions | subscription-create |
| PATCH /api/subscriptions/{id}/status | subscription-update-status |
| DELETE /api/subscriptions/{id} | subscription-delete |
| GET /api/dashboard/summary | dashboard-summary |
| GET /api/categories | category-list |
| POST /api/categories | category-create |
| POST /api/admin/reset-database | database-reset |
| PATCH /api/categories/{id}/name | category-rename |

---

## Support for New API Features

**Important Maintenance Note:**
Whenever new endpoints are added to the Subly API, the corresponding CLI verbs must be created as well. This ensures feature parity between the API and CLI interfaces.

For API extensions:
1. Create a new API endpoint following Clean Architecture principles
2. Create the corresponding CLI verb (using CommandLineParser)
3. Create API client methods (in the Services folder)
4. Update this documentation with examples
5. Update AGENTS.md with maintenance notes

---

## Help

For command-specific help:

```bash
dotnet run --project src/Subly.Cli -- --help
dotnet run --project src/Subly.Cli -- subscription-create --help
```


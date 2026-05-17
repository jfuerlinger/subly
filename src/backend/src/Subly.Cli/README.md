# Subly CLI

A command-line interface for the Subly subscription management system.

## Quick Start

```bash
# List subscriptions
dotnet run --project src/Subly.Cli -- subscription-list

# Create a subscription
dotnet run --project src/Subly.Cli -- subscription-create \
  --name "Netflix" \
  --vendor "Netflix Inc." \
  --category "streaming" \
  --price "15.99" \
  --cycle "monthly" \
  --next-payment "2026-06-17" \
  --payment-method "credit_card" \
  --started "2024-01-01"

# Get dashboard summary
dotnet run --project src/Subly.Cli -- dashboard-summary

# List categories
dotnet run --project src/Subly.Cli -- category-list
```

## API Connection

By default, the CLI connects to `http://localhost:5000`. To use a different server:

```bash
dotnet run --project src/Subly.Cli -- subscription-list --api-url https://api.example.com
```

## Full Documentation

See `SUBLY_CLI_SKILL.md` for complete usage documentation and examples.

## Architecture

- **Thin HTTP Client:** The CLI is a pure HTTP client to the API
- **No Business Logic:** All validation and logic is in the API layer
- **Feature Parity:** New API endpoints must have corresponding CLI verbs
- **Command-Line Parser:** Uses CommandLineParser library for verb handling


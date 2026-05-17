# AGENTS.md

## Documentation Lookup

When working on code in this repository, **always use Context7 MCP** to look up documentation for libraries and frameworks before writing or modifying code.

### Workflow

1. **Check the pre-resolved IDs** below first — if the library is listed, skip straight to step 3.
2. **Resolve unknown libraries** using `context7:resolve-library-id` with the package or framework name.
3. **Query the documentation** using `context7:query-docs` with the library ID and a specific topic query.
4. Use the returned documentation and code examples as the basis for your implementation.

### When to Use Context7

- Before implementing any feature that uses an external library or framework
- When writing API calls, hooks, configuration, or integration code
- When unsure about current API signatures, parameters, or best practices
- When upgrading or migrating library versions
- When generating boilerplate or scaffold code

### Pre-Resolved Library IDs

For the core technologies in this project, use these Context7 library IDs directly with `context7:query-docs` — no resolve step needed.

#### .NET

| Library ID | Description | Snippets | Score |
|---|---|---|---|
| `/dotnet/docs` | .NET Documentation (GitHub repo) | 32761 | 82.1 |
| `/websites/learn_microsoft_en-us_dotnet` | Microsoft Learn .NET docs (comprehensive) | 89429 | 71.4 |

#### .NET Aspire

| Library ID | Description | Snippets | Score |
|---|---|---|---|
| `/microsoft/aspire.dev` | Aspire.dev – official docs & integration catalog | 5015 | 88.8 |
| `/microsoft/aspire` | Aspire source repo – code-first examples | 3466 | 77.3 |
| `/dotnet/docs-aspire` | Conceptual Aspire documentation | 548 | 69.9 |

#### Entity Framework Core

| Library ID | Description | Snippets | Score |
|---|---|---|---|
| `/websites/learn_microsoft_en-us_ef_core` | Microsoft Learn EF Core docs | 4245 | 87.0 |
| `/dotnet/entityframework.docs` | EF Core docs repo | 3054 | 82.0 |

#### Vue.js

| Library ID | Description | Snippets | Score |
|---|---|---|---|
| `/websites/vuejs_guide` | Vue 3 Guide (Composition & Options API) | 695 | 85.2 |
| `/websites/vuejs_api` | Vue.js API Reference | 755 | 80.4 |
| `/llmstxt/vuejs_llms-full_txt` | Vue.js LLM-optimized full docs | 4480 | 79.8 |

> **Tip:** Pick the ID with the highest benchmark score for general questions. Use the larger-snippet sources for deep dives or uncommon APIs.

### Rules

- **Do not rely on training data** for library APIs — always verify via Context7 first.
- Prefer code examples returned by Context7 over memorized patterns.
- If Context7 returns no results for a library, fall back to general knowledge but note the limitation in a comment.
- When multiple library versions exist, query for the version specified in `package.json` or the project's dependency file.

## General Conventions

- Use metric/European units in all documentation and comments.
- Write commit messages in English.
- Prefer clear, readable code over clever one-liners.
- If `docker` CLI isn't available, use equivalent `podman` commands for container build/run checks.

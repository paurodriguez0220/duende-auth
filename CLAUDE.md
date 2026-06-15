# CLAUDE.md

## What this is

A personal reusable auth service (`DuendeAuth`) and a scaffolded API template (`ScalarApi`).
Every new personal project can register itself as a client in `DuendeAuth` and use `ScalarApi` as a starting point.

## Structure

```
src/
  DuendeAuth/          Duende IdentityServer — issues JWT tokens
  ScalarApi/           Protected API template — vertical slices, CQRS, Scalar UI
  ScalarApi.Tests/     xUnit tests for ScalarApi
```

## Commands

```bash
# Build everything
dotnet build duende.sln

# Run auth server (start this first — port 5001)
dotnet run --project src/DuendeAuth

# Run the API (Scalar UI at /scalar/v1)
dotnet run --project src/ScalarApi

# Run tests
dotnet test src/ScalarApi.Tests

# Docker (ScalarApi only)
docker build -f src/ScalarApi/Dockerfile -t scalar-api .
```

## Local setup (first time)

The `appsettings.Development.json` files are gitignored. Create them before running:

**`src/DuendeAuth/appsettings.Development.json`**
```json
{
  "Clients": { "ScalarClient": { "Secret": "dev-secret" } },
  "SeedUsers": { "AdminPassword": "Admin1234!" }
}
```

**`src/ScalarApi/appsettings.Development.json`** — only needed if overriding defaults.

## Conventions

- Follow `C:\Users\paulo.rodriguez\Paulo\standards` — these take precedence over any defaults.
- API routes are versioned: `/api/v1/...`
- Vertical slices: one folder per feature under `Features/`, each slice owns its Query/Command/Handler/Validator.
- Errors follow the shape: `{ "error": { "code": "...", "message": "...", "details": [] } }`
- Named constants live in `Common/Constants/` — no magic strings in code.
- Commits follow Conventional Commits: `feat(scope): description`

## Adding a new client to DuendeAuth

Edit `src/DuendeAuth/Config.cs` → add an entry to `GetClients()` with a new `ClientId`.
Add the corresponding secret to `appsettings.Development.json` (never hardcode it).

## Never do

- Hardcode secrets anywhere in source files — use config or environment variables.
- Add magic strings inline — define them in `PolicyNames` or equivalent constants.
- Commit `appsettings.Development.json` or `appsettings.Local.json`.
- Push directly to `main` — use feature branches and PRs.

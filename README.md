# DuendeAuth

A self-hosted OpenID Connect / OAuth 2.0 auth server built on [Duende IdentityServer](https://duendesoftware.com/products/identityserver). Designed to run once and be reused across all personal projects.

---

## Architecture decisions

### Why Duende IdentityServer?

Rolling your own auth is one of the most reliable ways to introduce security vulnerabilities. Duende implements the OpenID Connect and OAuth 2.0 specs correctly, is actively maintained, and is the direct successor to the widely-used IdentityServer4. For personal/non-commercial use the community license is free.

---

### Why is the DB provider abstracted? (`DbContextOptionsFactory`)

**The problem it solves:**

DuendeAuth is meant to be reused across different environments:
- Local dev → SQLite (zero setup, file-based)
- Shared server / production → Postgres or SQL Server (proper concurrent access, backups, etc.)

Without abstraction, switching databases means editing `Program.cs` every time — coupling infrastructure decisions to application code.

**The pattern used:**

`DbContextOptionsFactory` combines two patterns:

- **Factory Method** — a single method (`UseConfiguredProvider`) is responsible for constructing the right `DbContextOptionsBuilder` based on input. The caller never knows which provider it gets.
- **Strategy** — the `Database:Provider` config key selects the algorithm (which EF Core provider to wire up) at runtime. Adding a new provider means adding one `case` to the switch — nothing else changes.

**What this looks like in practice:**

```csharp
// Program.cs — caller knows nothing about SQLite, Postgres, or SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseConfiguredProvider(config, "IdentityConnection"));
```

```csharp
// DbContextOptionsFactory.cs — all provider knowledge lives here
return provider switch
{
    "postgres"  => builder.UseNpgsql(connStr, ...),
    "sqlserver" => builder.UseSqlServer(connStr, ...),
    _           => builder.UseSqlite(connStr, ...)
};
```

**To switch databases you only touch config, never code:**

```json
{ "Database": { "Provider": "postgres" } }
```

**Why not just use environment-specific `appsettings` files?**

You could put `UseNpgsql(...)` directly in `Program.cs` and swap it per environment. But then the code has a direct dependency on a specific provider — anyone reading it has to know that SQLite is for dev and Postgres for prod, and any new environment requires a code change and a redeploy. The factory keeps that concern in one place.

---

### Why two separate SQLite files (`duende-identity.db` and `duende-grants.db`)?

EF Core's `EnsureCreated()` only creates a database if it does not exist yet. When two `DbContext` classes share the same file, the first one to run creates the file and its own tables — the second one sees the file already exists and does nothing, leaving its tables missing.

Separate files mean each `DbContext` owns its database file and `EnsureCreated()` works correctly for both.

> For production use with Postgres or SQL Server, use proper EF Core migrations (`dotnet ef migrations add`) and separate schemas or databases per context.

---

### Why `AddDeveloperSigningCredential()`?

Duende's automatic key management stores signing keys in the `Keys` table (in the grants database). This feature requires a paid Business or Enterprise license.

`AddDeveloperSigningCredential()` instead writes a signing key to a local `tempkey.jwk` file and reads it back on restart. It requires no database table, no license, and is appropriate for development and personal use. It is **not suitable for production** because the key is not shared across multiple instances.

---

## Registering a new client app

1. Add a `Client` entry in `src/DuendeAuth/Config.cs` → `GetClients()`
2. Add its secret to `appsettings.Development.json` (gitignored)
3. In the new app, point `Auth:Authority` at `https://localhost:5001`

---

## Switching databases

| Provider | `Database:Provider` value |
|---|---|
| SQLite (default, dev) | `"sqlite"` |
| PostgreSQL | `"postgres"` |
| SQL Server | `"sqlserver"` |

Connection strings and provider value go in `appsettings.json`. Credentials go in `appsettings.Development.json` (gitignored) or environment variables — never hardcoded.

---

*Paulo Rodriguez — paulo.rodriguez@fefundinfo.com*

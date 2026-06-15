using DuendeAuth.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace DuendeAuth.Infrastructure;

public static class DbContextOptionsFactory
{
    public static DbContextOptionsBuilder UseConfiguredProvider(
        this DbContextOptionsBuilder builder,
        IConfiguration config,
        string connectionStringName,
        string? migrationsAssembly = null)
    {
        var provider = config[ConfigKeys.DatabaseProvider] ?? "sqlite";
        var connStr  = config.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' is not configured.");

        return provider.ToLowerInvariant() switch
        {
            DatabaseProviders.Postgres or DatabaseProviders.PostgreSQL => builder.UseNpgsql(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); }),
            DatabaseProviders.SqlServer => builder.UseSqlServer(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); }),
            _ => builder.UseSqlite(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); })
        };
    }
}

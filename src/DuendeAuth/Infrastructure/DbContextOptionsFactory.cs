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
        var provider  = config["Database:Provider"] ?? "sqlite";
        var connStr   = config.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' is not configured.");

        return provider.ToLowerInvariant() switch
        {
            "postgres" or "postgresql" => builder.UseNpgsql(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); }),
            "sqlserver" => builder.UseSqlServer(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); }),
            _ => builder.UseSqlite(connStr,
                sql => { if (migrationsAssembly is not null) sql.MigrationsAssembly(migrationsAssembly); })
        };
    }
}

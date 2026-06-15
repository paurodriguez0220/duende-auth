using DuendeAuth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DuendeAuth.Tests.Infrastructure;

public class DbContextOptionsFactoryTests
{
    private static IConfiguration BuildConfig(string? provider, string connStr) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["ConnectionStrings:TestConn"] = connStr
            })
            .Build();

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsSqlite_ConfiguresSqlite()
    {
        var config = BuildConfig("sqlite", "Data Source=test.db");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "SqliteOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsNotSet_DefaultsToSqlite()
    {
        var config = BuildConfig(null, "Data Source=test.db");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "SqliteOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsUnknown_DefaultsToSqlite()
    {
        var config = BuildConfig("mongodb", "Data Source=test.db");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "SqliteOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsPostgres_ConfiguresNpgsql()
    {
        var config = BuildConfig("postgres", "Host=localhost;Database=test");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "NpgsqlOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsPostgreSQL_ConfiguresNpgsql()
    {
        var config = BuildConfig("postgresql", "Host=localhost;Database=test");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "NpgsqlOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenProviderIsSqlServer_ConfiguresSqlServer()
    {
        var config = BuildConfig("sqlserver", "Server=localhost;Database=test;Trusted_Connection=True");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "SqlServerOptionsExtension");
    }

    [Theory]
    [InlineData("POSTGRES")]
    [InlineData("Postgres")]
    [InlineData("POSTGRESQL")]
    [InlineData("PostgreSQL")]
    public void UseConfiguredProvider_WhenPostgresProviderCaseVaries_ConfiguresNpgsql(string provider)
    {
        var config = BuildConfig(provider, "Host=localhost;Database=test");
        var builder = new DbContextOptionsBuilder();

        builder.UseConfiguredProvider(config, "TestConn");

        Assert.Contains(builder.Options.Extensions, e => e.GetType().Name == "NpgsqlOptionsExtension");
    }

    [Fact]
    public void UseConfiguredProvider_WhenConnectionStringMissing_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "sqlite" })
            .Build();
        var builder = new DbContextOptionsBuilder();

        var act = () => builder.UseConfiguredProvider(config, "TestConn");

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("TestConn", ex.Message);
    }
}

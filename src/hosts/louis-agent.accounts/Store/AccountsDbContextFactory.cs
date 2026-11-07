using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LouisAgent.Accounts.Store;

/// <summary>
/// How <c>dotnet ef migrations add</c> builds the context when it writes a migration: on <c>ACCOUNTS_SQL_URL</c> if it
/// is set, else on a placeholder, since writing a migration opens no connection.
/// </summary>
/// <remarks>
/// Design-time factory, EF Core's own seam for a context whose host isn't an app. The tool never runs in a host and
/// never connects, so the placeholder is safe; a real address is only needed to apply a migration, which the host
/// does at start-up.
/// </remarks>
public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    /// <summary>The setting the host reads the database from; the tool reads the same one.</summary>
    public const string Setting = "ACCOUNTS_SQL_URL";

    /// <summary>A connection string that names no server anyone could reach.</summary>
    private const string Placeholder = "Server=design-time;Database=accounts;Encrypt=true";

    /// <summary>The context on the configured server, or on the placeholder.</summary>
    public AccountsDbContext CreateDbContext(string[] args) =>
        new(Options(Environment.GetEnvironmentVariable(Setting) ?? Placeholder));

    /// <summary>The options for a context on this server.</summary>
    public static DbContextOptions<AccountsDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;
}

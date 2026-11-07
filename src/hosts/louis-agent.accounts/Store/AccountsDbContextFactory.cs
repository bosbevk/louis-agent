using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Accounts.Store;

/// <summary>
/// How <c>dotnet ef migrations add</c> builds the context when it writes a migration: on <c>ACCOUNTS_SQL_URL</c> if it
/// is set, else on a placeholder, since writing a migration opens no connection.
/// </summary>
/// <remarks>
/// Design-time factory, EF Core's own seam for a context whose host isn't an app. The tool never runs in a host and
/// never connects, so the placeholder is safe; a real address is only needed to apply a migration, which the host
/// does at start-up. Identity reads its schema version from the application's services when it builds the model, so
/// the options carry a small set of services naming the same version the host names (version 3, with passkeys), or
/// the migration would be written without the passkey table.
/// </remarks>
public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    /// <summary>The setting the host reads the database from; the tool reads the same one.</summary>
    public const string Setting = "ACCOUNTS_SQL_URL";

    /// <summary>The schema the host and the migrations agree on: version 3, with the passkey table (F61.5).</summary>
    public static readonly Version SchemaVersion = IdentitySchemaVersions.Version3;

    /// <summary>A connection string that names no server anyone could reach.</summary>
    private const string Placeholder = "Server=design-time;Database=accounts;Encrypt=true";

    /// <summary>Identity's options as the model builder reads them: the schema version, nothing else.</summary>
    private static readonly IServiceProvider IdentityServices = new ServiceCollection()
        .Configure<IdentityOptions>(identity => identity.Stores.SchemaVersion = SchemaVersion)
        .BuildServiceProvider();

    /// <summary>The context on the configured server, or on the placeholder.</summary>
    public AccountsDbContext CreateDbContext(string[] args) =>
        new(Options(Environment.GetEnvironmentVariable(Setting) ?? Placeholder));

    /// <summary>The options for a context on this server, outside a host.</summary>
    public static DbContextOptions<AccountsDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .UseApplicationServiceProvider(IdentityServices)
            .Options;
}

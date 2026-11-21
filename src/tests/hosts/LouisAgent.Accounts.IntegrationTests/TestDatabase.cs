using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// The SQL Server these tests run against, when <c>ACCOUNTS_SQL_TEST_URL</c> is set: the Docker <c>puls-sql</c>, or
/// CI's service. Each fixture names its own database, so dropping it hurts nothing.
/// </summary>
internal static class TestDatabase
{
    /// <summary>The setting that names the server.</summary>
    public const string Setting = "ACCOUNTS_SQL_TEST_URL";

    /// <summary>Where SQL Server runs, if this machine has one.</summary>
    public static string? Url => Environment.GetEnvironmentVariable(Setting);

    /// <summary>A clock the tests agree on.</summary>
    public static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Skips the fixture when there's no server to talk to.</summary>
    public static void Needed() =>
        Outside.Tool(!string.IsNullOrEmpty(Url), $"{Setting} isn't set: the accounts database isn't tested here");

    /// <summary>The context on the fixture's database: the server from the setting, the database its own.</summary>
    public static AccountsDbContext Open(string database)
    {
        var address = new SqlConnectionStringBuilder(Url) { InitialCatalog = database };
        return new AccountsDbContext(AccountsDbContextFactory.Options(address.ConnectionString));
    }

    /// <summary>Builds the fixture's database from nothing with the migrations, with these tenants in it.</summary>
    public static async Task ResetAsync(string database, params Tenant[] tenants)
    {
        await using var context = Open(database);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        context.Tenants.AddRange(tenants);
        await context.SaveChangesAsync();
    }

    /// <summary>Drops the fixture's database, if there was a server to make it on.</summary>
    public static async Task DropAsync(string database)
    {
        if (string.IsNullOrEmpty(Url)) return;
        await using var context = Open(database);
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>A user as Identity's manager would make it: the normalised columns filled in upper case.</summary>
    public static AccountUser User(string tenant, string email)
    {
        var user = AccountUser.New(tenant, email, Now);
        user.NormalizedUserName = email.ToUpperInvariant();
        user.NormalizedEmail = email.ToUpperInvariant();
        return user;
    }
}

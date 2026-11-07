using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.IntegrationTests.Store;

/// <summary>
/// The migrations build the accounts database on a real SQL Server, and the per-tenant rule holds there: two tenants
/// can each hold one email, one tenant can't hold it twice. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set; a
/// database of its own, dropped at the end.
/// </summary>
[Category("Integration")]
public class AccountsDatabaseTests
{
    /// <summary>Where SQL Server runs, if this machine has one: the Docker <c>puls-sql</c>, or CI's service.</summary>
    private static string? Url => Environment.GetEnvironmentVariable("ACCOUNTS_SQL_TEST_URL");

    /// <summary>A database only these tests use, so dropping it hurts nothing.</summary>
    private const string Database = "puls_accounts_test";

    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Skips the fixture without a server, then builds the database from nothing with the migrations.</summary>
    [OneTimeSetUp]
    public async Task MigrateAnEmptyDatabase()
    {
        Outside.Tool(!string.IsNullOrEmpty(Url),
            "ACCOUNTS_SQL_TEST_URL isn't set: the accounts database isn't tested here");

        await using var context = Open();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        context.Tenants.AddRange(new Tenant
        {
            Id = "home",
            Name = "Home"
        },
            new Tenant
            {
                Id = "acme",
                Name = "Acme",
                Host = "acme.example"
            });
        await context.SaveChangesAsync();
    }

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await using var context = Open();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Two tenants can each hold ann@example.com: the email is unique within a tenant, not across them.</summary>
    [Test]
    public async Task TheSameEmail_FitsInTwoTenants()
    {
        await using var context = Open();
        context.Users.AddRange(User("home", "ann@example.com"), User("acme", "ann@example.com"));

        await context.SaveChangesAsync();

        Assert.That(await context.Users.CountAsync(u => u.NormalizedEmail == "ANN@EXAMPLE.COM"), Is.EqualTo(2));
    }

    /// <summary>One tenant can't hold the same email twice: the database refuses the second row.</summary>
    [Test]
    public async Task TheSameEmail_IsRefusedTwiceInOneTenant()
    {
        await using var context = Open();
        context.Users.AddRange(User("home", "bob@example.com"), User("home", "bob@example.com"));

        Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>A user names a tenant that must exist.</summary>
    [Test]
    public async Task AUser_InATenantThatDoesNotExist_IsRefused()
    {
        await using var context = Open();
        context.Users.Add(User("nobody", "cat@example.com"));

        Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>A user as Identity's manager would make it: the normalised columns filled in upper case.</summary>
    private static AccountUser User(string tenant, string email)
    {
        var user = AccountUser.New(tenant, email, Now);
        user.NormalizedUserName = email.ToUpperInvariant();
        user.NormalizedEmail = email.ToUpperInvariant();
        return user;
    }

    /// <summary>The context on the test database: the server from the setting, the database ours.</summary>
    private static AccountsDbContext Open()
    {
        var address = new SqlConnectionStringBuilder(Url) { InitialCatalog = Database };
        return new AccountsDbContext(AccountsDbContextFactory.Options(address.ConnectionString));
    }
}

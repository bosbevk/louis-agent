using LouisAgent.Accounts.Users;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Store;

/// <summary>
/// The migrations build the accounts database on a real SQL Server, and the per-tenant rule holds there: two tenants
/// can each hold one email, one tenant can't hold it twice. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set; a
/// database of its own, dropped at the end.
/// </summary>
[Category("Integration")]
public class AccountsDatabaseTests
{
    /// <summary>A database only these tests use.</summary>
    private const string Database = "puls_accounts_test";

    /// <summary>Skips the fixture without a server, then builds the database from nothing with the migrations.</summary>
    [OneTimeSetUp]
    public async Task MigrateAnEmptyDatabase()
    {
        Needed();
        await ResetAsync(Database, new Tenant
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
    }

    /// <summary>Cleans up after the tests by dropping their database.</summary>
    [OneTimeTearDown]
    public Task DropTheTestDatabase() => DropAsync(Database);

    /// <summary>Two tenants can each hold ann@example.com: the email is unique within a tenant, not across them.</summary>
    [Test]
    public async Task TheSameEmail_FitsInTwoTenants()
    {
        await using var context = Open(Database);
        context.Users.AddRange(User("home", "ann@example.com"), User("acme", "ann@example.com"));

        await context.SaveChangesAsync();

        Assert.That(await context.Users.CountAsync(u => u.NormalizedEmail == "ANN@EXAMPLE.COM"), Is.EqualTo(2));
    }

    /// <summary>One tenant can't hold the same email twice: the database refuses the second row.</summary>
    [Test]
    public async Task TheSameEmail_IsRefusedTwiceInOneTenant()
    {
        await using var context = Open(Database);
        context.Users.AddRange(User("home", "bob@example.com"), User("home", "bob@example.com"));

        Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>A user names a tenant that must exist.</summary>
    [Test]
    public async Task AUser_InATenantThatDoesNotExist_IsRefused()
    {
        await using var context = Open(Database);
        context.Users.Add(User("nobody", "cat@example.com"));

        Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}

using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using Microsoft.Extensions.Time.Testing;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Store;

/// <summary>
/// The store finds a person within the request's tenant only, and stamps a new account with the tenant and the time,
/// the way Identity's register route makes one. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class AccountUserStoreDatabaseTests
{
    private const string Database = "puls_accounts_test_store";

    [OneTimeSetUp]
    public async Task BuildTheDatabase()
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
                Name = "Acme"
            });

        // Ann has an account in both tenants, with the same email.
        await using var db = Open(Database);
        db.Users.AddRange(User("home", "ann@example.com"), User("acme", "ann@example.com"));
        await db.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public Task DropTheTestDatabase() => DropAsync(Database);

    /// <summary>A store for a request that landed in this tenant.</summary>
    private static AccountUserStore StoreIn(AccountsDbContext db, string tenant)
    {
        var current = new CurrentTenant();
        current.Set(tenant);
        return new AccountUserStore(db, current, new FakeTimeProvider(Now));
    }

    /// <summary>Looking Ann up in acme gives acme's Ann, not home's.</summary>
    [Test]
    public async Task FindByEmail_FindsTheTenantsOwnUser()
    {
        await using var db = Open(Database);

        var found = await StoreIn(db, "acme").FindByEmailAsync("ANN@EXAMPLE.COM");

        Assert.That(found?.TenantId, Is.EqualTo("acme"));
    }

    /// <summary>A tenant where Ann has no account finds nobody, though the email exists elsewhere.</summary>
    [Test]
    public async Task FindByName_InATenantWithoutTheUser_FindsNobody()
    {
        await using var db = Open(Database);
        db.Tenants.Add(new Tenant
        {
            Id = "other",
            Name = "Other"
        });
        await db.SaveChangesAsync();

        Assert.That(await StoreIn(db, "other").FindByNameAsync("ANN@EXAMPLE.COM"), Is.Null);
    }

    /// <summary>A user made the way Identity's register route makes one lands in the request's tenant, timed.</summary>
    [Test]
    public async Task Create_StampsTheTenantAndTheTime()
    {
        await using var db = Open(Database);
        var user = new AccountUser
        {
            UserName = "bob@example.com",
            Email = "bob@example.com",
            NormalizedUserName = "BOB@EXAMPLE.COM",
            NormalizedEmail = "BOB@EXAMPLE.COM"
        };

        var result = await StoreIn(db, "home").CreateAsync(user);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(user.TenantId, Is.EqualTo("home"));
            Assert.That(user.CreatedAt, Is.EqualTo(Now));
        });
    }
}

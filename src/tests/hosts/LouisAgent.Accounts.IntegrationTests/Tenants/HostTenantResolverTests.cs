using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Tenants;

/// <summary>
/// A request lands in the tenant whose row names its host, however the host is written, and in the default tenant
/// for every other host. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class HostTenantResolverTests
{
    private const string Database = "puls_accounts_test_tenants";

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
                Name = "Acme",
                Host = "acme.example"
            });
    }

    [OneTimeTearDown]
    public Task DropTheTestDatabase() => DropAsync(Database);

    /// <summary>The row's host wins, in any case, with a port, or behind www.</summary>
    [TestCase("acme.example")]
    [TestCase("WWW.Acme.Example:8080")]
    public async Task AHostARowNames_LandsInThatTenant(string host)
    {
        await using var db = Open(Database);
        var resolver = new HostTenantResolver(db, "home");

        Assert.That(await resolver.ResolveAsync(host), Is.EqualTo("acme"));
    }

    /// <summary>Any other host, and no host at all, is the instance's default tenant.</summary>
    [TestCase("localhost:5071")]
    [TestCase("mypuls.example")]
    [TestCase("")]
    [TestCase(null)]
    public async Task AnyOtherHost_LandsInTheDefaultTenant(string? host)
    {
        await using var db = Open(Database);
        var resolver = new HostTenantResolver(db, "home");

        Assert.That(await resolver.ResolveAsync(host), Is.EqualTo("home"));
    }
}

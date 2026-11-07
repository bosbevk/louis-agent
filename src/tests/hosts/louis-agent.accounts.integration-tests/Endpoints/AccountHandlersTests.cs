using System.Net;
using System.Net.Http.Json;
using LouisAgent.Accounts.Contracts;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints;

/// <summary>
/// Our routes beside Identity's, through the test host on SQL Server: who am I, my name, sign out, delete me, and
/// the providers' list. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class AccountHandlersTests
{
    private const string Database = "puls_accounts_test_routes";
    private AccountsTestHost _host = null!;

    [OneTimeSetUp]
    public async Task StartSignedIn()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database);
        await _host.SignUpAsync("ann@example.com", "Long-enough-1");
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    [Test, Order(1)]
    public async Task Me_SaysWhoIsSignedIn_AndInWhichTenant()
    {
        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(me!.Email, Is.EqualTo("ann@example.com"));
            Assert.That(me.TenantId, Is.EqualTo("home"));
            Assert.That(me.EmailConfirmed, Is.True);
            Assert.That(me.HasPassword, Is.True);
            Assert.That(me.Logins, Is.Empty);
            Assert.That(me.DisplayName, Is.Null);
        });
    }

    [Test, Order(2)]
    public async Task Profile_ChangesTheName_AndBlankClearsIt()
    {
        using var named = await _host.Client.PutAsJsonAsync("/account/profile", new ProfileBody("  Ann  "),
            AccountJson.Web);
        var withName = await named.Content.ReadFromJsonAsync<AccountInfo>(AccountJson.Web);
        using var cleared = await _host.Client.PutAsJsonAsync("/account/profile", new ProfileBody(" "),
            AccountJson.Web);
        var without = await cleared.Content.ReadFromJsonAsync<AccountInfo>(AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(withName!.DisplayName, Is.EqualTo("Ann"));
            Assert.That(without!.DisplayName, Is.Null);
        });
    }

    [Test, Order(3)]
    public async Task Profile_RefusesANameTooLong_WithTheReason()
    {
        using var refused = await _host.Client.PutAsJsonAsync("/account/profile",
            new ProfileBody(new string('x', 257)), AccountJson.Web);

        Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await refused.Content.ReadAsStringAsync(), Does.Contain("A name is at most 256 characters."));
    }

    /// <summary>Every provider is listed, none on: the test host has no keys.</summary>
    [Test, Order(4)]
    public async Task Providers_IsOpen_AndListsEveryProvider_NoneOnWithoutKeys()
    {
        using var plain = _host.PlainClient();

        var reply = await plain.GetFromJsonAsync<ProvidersReply>("/account/providers", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(reply!.Providers.Select(p => p.Name), Does.Contain("google").And.Contain("microsoft"));
            Assert.That(reply.Providers.All(p => !p.Enabled), Is.True);
        });
    }

    [Test, Order(5)]
    public async Task Logout_EndsTheSignIn()
    {
        using var gone = await _host.Client.PostAsync("/account/logout", null);
        using var me = await _host.Client.GetAsync("/account/me");

        Assert.Multiple(() =>
        {
            Assert.That(gone.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(me.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    [Test, Order(6)]
    public async Task Delete_RemovesTheAccount_AndSignsOut()
    {
        await _host.SignInAsync("ann@example.com", "Long-enough-1");

        using var deleted = await _host.Client.DeleteAsync("/account");
        using var me = await _host.Client.GetAsync("/account/me");
        await using var db = Open(Database);

        Assert.Multiple(async () =>
        {
            Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(me.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(await db.Users.AnyAsync(u => u.NormalizedEmail == "ANN@EXAMPLE.COM"), Is.False);
        });
    }
}

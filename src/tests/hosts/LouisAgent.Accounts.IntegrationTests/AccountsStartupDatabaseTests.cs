using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// A host built with the three start-up calls works end to end on a real SQL Server: it builds the database and the
/// default tenant as it starts, a person registers, confirms from the mail, signs in with a cookie and reads their
/// own info, and the account is in the default tenant. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class AccountsStartupDatabaseTests
{
    private const string Database = "puls_accounts_test_startup";
    private AccountsTestHost _host = null!;

    [OneTimeSetUp]
    public async Task StartOnAnEmptyDatabase()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database);
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    /// <summary>Start-up made the tables and the default tenant's row.</summary>
    [Test]
    public async Task StartUp_BuildsTheDatabase_WithTheDefaultTenant()
    {
        await using var db = Open(Database);

        Assert.That(await db.Tenants.AnyAsync(t => t.Id == "home"), Is.True);
    }

    /// <summary>A person can't sign in before the mail's link confirmed them, and can after.</summary>
    [Test]
    public async Task Register_Confirm_SignIn_ReadInfo()
    {
        var client = _host.Client;
        using var registered = await client.PostAsJsonAsync("/account/register",
            new
            {
                email = "ann@example.com",
                password = "Long-enough-1"
            });
        Assert.That(registered.StatusCode, Is.EqualTo(HttpStatusCode.OK), await registered.Content.ReadAsStringAsync());

        using var tooEarly = await client.PostAsJsonAsync("/account/login?useCookies=true",
            new
            {
                email = "ann@example.com",
                password = "Long-enough-1"
            });
        Assert.That(tooEarly.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "not confirmed yet");

        using var confirmed = await client.GetAsync(_host.Mail.Links.Single());
        Assert.That(confirmed.StatusCode, Is.EqualTo(HttpStatusCode.OK), await confirmed.Content.ReadAsStringAsync());
        using var signedIn = await client.PostAsJsonAsync("/account/login?useCookies=true",
            new
            {
                email = "ann@example.com",
                password = "Long-enough-1"
            });
        Assert.That(signedIn.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var info = await client.GetAsync("/account/manage/info");
        var body = await info.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Multiple(() =>
        {
            Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body.GetProperty("email").GetString(), Is.EqualTo("ann@example.com"));
            Assert.That(body.GetProperty("isEmailConfirmed").GetBoolean(), Is.True);
        });

        await using var db = Open(Database);
        var ann = await db.Users.SingleAsync(u => u.NormalizedEmail == "ANN@EXAMPLE.COM");
        Assert.That(ann.TenantId, Is.EqualTo("home"), "registered through Identity's route, stamped by our store");
    }

    /// <summary>Without the cookie, a route that needs a person answers 401, never a page.</summary>
    [Test]
    public async Task WithoutACookie_ManageInfo_Is401()
    {
        using var plain = new HttpClient(_host.App.GetTestServer().CreateHandler())
        {
            BaseAddress = _host.App.GetTestServer().BaseAddress
        };

        using var info = await plain.GetAsync("/account/manage/info");

        Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

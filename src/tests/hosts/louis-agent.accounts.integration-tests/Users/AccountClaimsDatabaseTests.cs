using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Users;

/// <summary>
/// The sign-in cookie carries the tenant, so a host reads who opened a session and where from the cookie alone.
/// Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class AccountClaimsDatabaseTests
{
    private const string Database = "puls_accounts_test_claims";
    private AccountsTestHost _host = null!;

    [OneTimeSetUp]
    public async Task StartSignedIn()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        // A route of the host's own that says what the cookie's claims are, as the API's session route reads them.
        _host = await AccountsTestHost.StartAsync(Database, map: app => app.MapGet("/whoami",
            (ClaimsPrincipal p) =>
                $"{p.FindFirstValue(ClaimTypes.NameIdentifier)} in {p.FindFirstValue(AccountClaims.Tenant)}"));
        await _host.SignUpAsync("ann@example.com", "Long-enough-1");
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    [Test]
    public async Task TheCookie_NamesTheUserAndTheTenant()
    {
        await using var db = Open(Database);
        var ann = await db.Users.SingleAsync(u => u.NormalizedEmail == "ANN@EXAMPLE.COM");

        Assert.That(await _host.Client.GetStringAsync("/whoami"), Is.EqualTo($"{ann.Id} in home"));
    }
}

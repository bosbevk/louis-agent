using System.Net;
using System.Net.Http.Json;
using LouisAgent.Accounts.Contracts;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints;

/// <summary>
/// Signing in through a provider, on a scripted one: a first sign-in makes a confirmed account in the tenant, a second
/// finds it, a known email joins its account, linking and unlinking work, and the last way in can't be taken away
/// without a password. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class ExternalLoginTests
{
    private const string Database = "puls_accounts_test_external";
    private AccountsTestHost _host = null!;

    [OneTimeSetUp]
    public async Task Start()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database, map: ScriptedProvider.Map);
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    /// <summary>The provider says who this is, then the callback runs.</summary>
    private async Task<HttpResponseMessage> ComeBackFromAsync(string provider, string key, string? email,
        string? name = null, string? returnUrl = "/gesels", bool link = false, string? xsrf = null)
    {
        string script = $"/test/external?provider={provider}&key={key}" +
                        (email is null ? "" : $"&email={Uri.EscapeDataString(email)}") +
                        (name is null ? "" : $"&name={Uri.EscapeDataString(name)}") +
                        (xsrf is null ? "" : $"&xsrf={xsrf}");
        using var said = await _host.Client.GetAsync(script);
        said.EnsureSuccessStatusCode();
        return await _host.Client.GetAsync(
            $"/account/external/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "")}&link={link}");
    }

    [Test, Order(1)]
    public async Task AFirstSignIn_MakesAConfirmedAccount_InTheTenant_AndGoesBack()
    {
        using var back = await ComeBackFromAsync("google", "g-1", "ann@example.com", "Ann Example");

        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);
        Assert.Multiple(() =>
        {
            Assert.That(back.StatusCode, Is.EqualTo(HttpStatusCode.Found));
            Assert.That(back.Headers.Location?.ToString(), Is.EqualTo("/gesels"));
            Assert.That(me!.Email, Is.EqualTo("ann@example.com"));
            Assert.That(me.TenantId, Is.EqualTo("home"));
            Assert.That(me.EmailConfirmed, Is.True);
            Assert.That(me.DisplayName, Is.EqualTo("Ann Example"));
            Assert.That(me.Logins, Is.EqualTo(new[] { "google" }));
            Assert.That(me.HasPassword, Is.False);
        });
    }

    [Test, Order(2)]
    public async Task TheLastWayIn_CannotBeUnlinked_WithoutAPassword()
    {
        using var refused = await _host.Client.DeleteAsync("/account/external/google");

        Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await refused.Content.ReadAsStringAsync(), Does.Contain("last way into the account"));
    }

    [Test, Order(3)]
    public async Task APassword_CanBeSet_Once()
    {
        using var set = await _host.Client.PostAsJsonAsync("/account/password", new SetPasswordBody("Long-enough-1"),
            AccountJson.Web);
        using var again = await _host.Client.PostAsJsonAsync("/account/password", new SetPasswordBody("Other-pass-2"),
            AccountJson.Web);
        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(set.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(again.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(me!.HasPassword, Is.True);
        });
    }

    [Test, Order(4)]
    public async Task Linking_NeedsTheSamePerson_ThenAddsTheProvider()
    {
        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        using var linked = await ComeBackFromAsync("microsoft", "m-1", "ann@example.com", link: true, xsrf: me!.UserId);
        var after = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(linked.Headers.Location?.ToString(), Is.EqualTo("/profile"));
            Assert.That(after!.Logins, Is.EquivalentTo(new[] { "google", "microsoft" }));
        });
    }

    [Test, Order(5)]
    public async Task Unlinking_TakesTheProviderOff_WhenAnotherWayInRemains()
    {
        using var gone = await _host.Client.DeleteAsync("/account/external/microsoft");
        var after = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(gone.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(after!.Logins, Is.EqualTo(new[] { "google" }));
        });
    }

    [Test, Order(6)]
    public async Task ASecondSignIn_FindsTheAccount_AndAProviderWithoutAnEmail_IsRefused()
    {
        using var out1 = await _host.Client.PostAsync("/account/logout", null);
        using var again = await ComeBackFromAsync("google", "g-1", null, returnUrl: "//evil.test");
        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);
        using var out2 = await _host.Client.PostAsync("/account/logout", null);
        using var nobody = await ComeBackFromAsync("google", "g-new", null);

        Assert.Multiple(() =>
        {
            Assert.That(again.Headers.Location?.ToString(), Is.EqualTo("/"),
                "a known login signs in; the return address is checked");
            Assert.That(me!.Email, Is.EqualTo("ann@example.com"));
            Assert.That(nobody.Headers.Location?.ToString(), Is.EqualTo("/login?failed=external"));
        });
    }

    /// <summary>A provider this instance has no keys for has no challenge; one it has sends the browser to it.</summary>
    [Test, Order(7)]
    public async Task TheChallenge_IsForConfiguredProvidersOnly()
    {
        using var plain = _host.PlainClient();

        using var missing = await plain.GetAsync("/account/external/google");

        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await using var db = Open(Database);
        Assert.That(await db.Users.CountAsync(), Is.EqualTo(1), "every sign-in above was Ann");
    }
}

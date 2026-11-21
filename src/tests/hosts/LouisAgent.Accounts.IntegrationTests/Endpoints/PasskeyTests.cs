using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Accounts.Contracts;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints;

/// <summary>
/// A passkey from a software authenticator: made from Identity's options and kept under a name, listed, used to
/// sign in from a signed-out browser, and removed. Runs when <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class PasskeyTests
{
    private const string Database = "puls_accounts_test_passkeys";
    private const string Origin = "http://localhost";
    private AccountsTestHost _host = null!;
    private readonly SoftwareAuthenticator _authenticator = new();
    private string? _userHandle;

    [OneTimeSetUp]
    public async Task StartSignedIn()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database);
        // A browser says where a call came from; Identity checks the credential's origin against it.
        _host.Client.DefaultRequestHeaders.Add("Origin", Origin);
        await _host.SignUpAsync("ann@example.com", "Long-enough-1");
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        _authenticator.Dispose();
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    [Test, Order(1)]
    public async Task APasskey_IsMadeFromTheOptions_AndKeptUnderItsName()
    {
        using var options = await _host.Client.PostAsync("/account/passkeys/options", null);
        string optionsJson = await options.Content.ReadAsStringAsync();
        using (var parsed = JsonDocument.Parse(optionsJson))
            _userHandle = parsed.RootElement.GetProperty("user").GetProperty("id").GetString();

        string credential = _authenticator.Create(optionsJson, Origin);
        using var added = await _host.Client.PostAsJsonAsync("/account/passkeys",
            new PasskeyBody(credential, "  Laptop "), AccountJson.Web);
        var list = await _host.Client.GetFromJsonAsync<PasskeysReply>("/account/passkeys", AccountJson.Web);

        string refusal = await added.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(options.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(added.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), refusal);
            Assert.That(list!.Passkeys.Select(p => (p.Id, p.Name)),
                Is.EqualTo(new[] { (_authenticator.CredentialId, "Laptop") }));
        });
    }

    /// <summary>A credential that answers no options of ours is refused with the reason.</summary>
    [Test, Order(2)]
    public async Task ACredentialThatAnswersNothing_IsRefused()
    {
        using var stale = new SoftwareAuthenticator();
        string credential = stale.Create(
            """{"challenge":"AAAA","rp":{"id":"localhost"},"user":{"id":"x"}}""", Origin);

        using var refused = await _host.Client.PostAsJsonAsync("/account/passkeys",
            new PasskeyBody(credential, null), AccountJson.Web);

        Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await refused.Content.ReadAsStringAsync(), Does.Contain("couldn't be added"));
    }

    [Test, Order(3)]
    public async Task SignedOut_ThePasskey_SignsIn()
    {
        using var signedOut = await _host.Client.PostAsync("/account/logout", null);
        using var gone = await _host.Client.GetAsync("/account/me");
        using var options =
            await _host.Client.PostAsync("/account/passkeys/request-options?email=ann%40example.com", null);
        string credential = _authenticator.Get(await options.Content.ReadAsStringAsync(), Origin, _userHandle);

        using var signedIn = await _host.Client.PostAsJsonAsync("/account/passkeys/login",
            new PasskeyLoginBody(credential), AccountJson.Web);
        var me = await _host.Client.GetFromJsonAsync<AccountInfo>("/account/me", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(gone.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(signedIn.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(me!.Email, Is.EqualTo("ann@example.com"));
        });
    }

    [Test, Order(4)]
    public async Task ASecondSignature_WithTheSameCounter_IsRefused()
    {
        using var signedOut = await _host.Client.PostAsync("/account/logout", null);
        using var options = await _host.Client.PostAsync("/account/passkeys/request-options", null);
        string credential = _authenticator.Get(await options.Content.ReadAsStringAsync(), Origin, _userHandle);
        // The authenticator counts each signature; a replayed one would carry a count the server has seen.
        using var signedIn = await _host.Client.PostAsJsonAsync("/account/passkeys/login",
            new PasskeyLoginBody(credential), AccountJson.Web);

        Assert.That(signedIn.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a fresh signature, no email given");
    }

    [Test, Order(5)]
    public async Task Removing_ThePasskey_TakesItOff()
    {
        using var removed = await _host.Client.DeleteAsync($"/account/passkeys/{_authenticator.CredentialId}");
        using var again = await _host.Client.DeleteAsync($"/account/passkeys/{_authenticator.CredentialId}");
        var list = await _host.Client.GetFromJsonAsync<PasskeysReply>("/account/passkeys", AccountJson.Web);

        Assert.Multiple(() =>
        {
            Assert.That(removed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(again.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(list!.Passkeys, Is.Empty);
        });
    }
}

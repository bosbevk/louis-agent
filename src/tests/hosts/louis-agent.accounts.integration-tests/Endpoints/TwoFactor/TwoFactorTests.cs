using System.Net;
using System.Net.Http.Json;
using LouisAgent.Accounts.Contracts;
using Microsoft.EntityFrameworkCore;
using static LouisAgent.Accounts.IntegrationTests.TestDatabase;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints.TwoFactor;

/// <summary>
/// Two-factor through Identity's own route on our host: the key, turning it on with the app's code, the password
/// alone refused after that, the code or a recovery code letting the person in, and turning it off again. Runs when
/// <c>ACCOUNTS_SQL_TEST_URL</c> is set.
/// </summary>
[Category("Integration")]
public class TwoFactorTests
{
    private const string Database = "puls_accounts_test_2fa";
    private const string Email = "ann@example.com", Password = "Long-enough-1";
    private AccountsTestHost _host = null!;
    private string _key = "", _recoveryCode = "";

    [OneTimeSetUp]
    public async Task StartSignedIn()
    {
        Needed();
        await using (var db = Open(Database))
            await db.Database.EnsureDeletedAsync();
        _host = await AccountsTestHost.StartAsync(Database);
        await _host.SignUpAsync(Email, Password);
    }

    [OneTimeTearDown]
    public async Task StopAndDrop()
    {
        if (_host is not null) await _host.DisposeAsync();
        await DropAsync(Database);
    }

    [Test, Order(1)]
    public async Task BeforeItIsOn_TheStateHasAKey_AndIsOff()
    {
        var state = await TwoFactorAsync(new TwoFactorBody());

        Assert.Multiple(() =>
        {
            Assert.That(state.IsTwoFactorEnabled, Is.False);
            Assert.That(state.SharedKey, Has.Length.EqualTo(32), "twenty random bytes in base32");
            Assert.That(state.RecoveryCodes, Is.Null);
        });
        _key = state.SharedKey;
    }

    [Test, Order(2)]
    public async Task TurnedOnWithTheAppsCode_GivesTenRecoveryCodes()
    {
        var state = await TwoFactorAsync(new TwoFactorBody(Enable: true, TwoFactorCode: Totp.Now(_key)));

        Assert.Multiple(() =>
        {
            Assert.That(state.IsTwoFactorEnabled, Is.True);
            Assert.That(state.RecoveryCodes, Has.Count.EqualTo(10));
            Assert.That(state.RecoveryCodesLeft, Is.EqualTo(10));
        });
        _recoveryCode = state.RecoveryCodes![0];
    }

    [Test, Order(3)]
    public async Task ThePasswordAlone_IsRefused_AskingForTheSecondFactor()
    {
        using var client = _host.PlainClient();

        using var response =
            await client.PostAsJsonAsync("/account/login?useCookies=true", new LoginBody(Email, Password));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("RequiresTwoFactor"));
    }

    [Test, Order(4)]
    public async Task ThePasswordWithTheAppsCode_SignsIn()
    {
        using var client = _host.PlainClient();

        using var response = await client.PostAsJsonAsync("/account/login?useCookies=true",
            new LoginBody(Email, Password, TwoFactorCode: Totp.Now(_key)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test, Order(5)]
    public async Task ARecoveryCode_SignsInOnce()
    {
        using var client = _host.PlainClient();
        var body = new LoginBody(Email, Password, TwoFactorRecoveryCode: _recoveryCode);

        using var first = await client.PostAsJsonAsync("/account/login?useCookies=true", body);
        using var again = await client.PostAsJsonAsync("/account/login?useCookies=true", body);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(again.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "a recovery code is spent");
        });
    }

    [Test, Order(6)]
    public async Task TurnedOff_ThePasswordAloneSignsInAgain()
    {
        var state = await TwoFactorAsync(new TwoFactorBody(Enable: false));
        using var client = _host.PlainClient();

        using var response =
            await client.PostAsJsonAsync("/account/login?useCookies=true", new LoginBody(Email, Password));

        Assert.Multiple(() =>
        {
            Assert.That(state.IsTwoFactorEnabled, Is.False);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>The signed-in person's call to Identity's route, read as our reply record.</summary>
    private async Task<TwoFactorReply> TwoFactorAsync(TwoFactorBody body)
    {
        using var response = await _host.Client.PostAsJsonAsync("/account/manage/2fa", body, AccountJson.Web);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwoFactorReply>(AccountJson.Web))!;
    }
}

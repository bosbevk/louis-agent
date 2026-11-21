using System.Net;
using System.Security.Claims;
using System.Text;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;

namespace LouisAgent.Web.Accounts.Tests.Client;

/// <summary>The state is the one truth about who is signed in, and every change is announced once.</summary>
public class AccountStateTests
{
    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", "Ann", true, [], true);

    /// <summary>A state over an API whose every route answers this.</summary>
    private static AccountState State(HttpStatusCode status, string json = "") =>
        new(new AccountApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }))
        { BaseAddress = new Uri("http://localhost/") }));

    [Test]
    public async Task Load_ReadsThePerson_AndAnnounces()
    {
        var state = State(HttpStatusCode.OK,
            """{"userId":"1","tenantId":"home","email":"ann@x.test","displayName":"Ann","emailConfirmed":true,"logins":[],"hasPassword":true,"providers":[]}""");
        int announced = 0;
        state.Changed += () => announced++;

        await state.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(state.Enabled, Is.True);
            Assert.That(state.Me?.Email, Is.EqualTo(Ann.Email));
            Assert.That(state.SignedIn, Is.True);
            Assert.That(announced, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Load_OnAnInstanceWithoutAccounts_IsAGuest_WithAccountsOff()
    {
        var state = State(HttpStatusCode.NotFound);

        await state.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(state.Enabled, Is.False);
            Assert.That(state.SignedIn, Is.False);
            Assert.That(state.Loaded, Is.True);
        });
    }

    [Test]
    public void Set_AndClear_Announce()
    {
        var state = State(HttpStatusCode.NotFound);
        var seen = new List<bool>();
        state.Changed += () => seen.Add(state.SignedIn);

        state.Set(Ann);
        state.Clear();

        Assert.That(seen, Is.EqualTo(new[] { true, false }));
    }

    /// <summary>Blazor's principal carries the same claims the server's cookie does.</summary>
    [Test]
    public void ThePrincipal_HasTheCookiesClaims()
    {
        var principal = CookieAuthenticationStateProvider.PrincipalOf(Ann);

        Assert.Multiple(() =>
        {
            Assert.That(principal.Identity!.IsAuthenticated, Is.True);
            Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("1"));
            Assert.That(principal.FindFirst(ClaimTypes.Name)?.Value, Is.EqualTo("Ann"));
            Assert.That(principal.FindFirst(AccountClaims.Tenant)?.Value, Is.EqualTo("home"));
            Assert.That(CookieAuthenticationStateProvider.PrincipalOf(null).Identity!.IsAuthenticated, Is.False);
        });
    }
}

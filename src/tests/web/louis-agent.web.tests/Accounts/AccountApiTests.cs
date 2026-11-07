using System.Net;
using System.Text;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Api.Contracts;
using LouisAgent.Web.Accounts;

namespace LouisAgent.Web.Tests.Accounts;

/// <summary>
/// The account client sends the header that makes the cookie count on every call, reads a guest as null, and turns
/// a refusal into an exception with what the route said.
/// </summary>
public class AccountApiTests
{
    private static (AccountApi Api, StubHandler Handler) Api(HttpStatusCode status, string json = "")
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        return (new AccountApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }), handler);
    }

    [Test]
    public async Task EveryCall_CarriesTheHeader()
    {
        var (api, handler) = Api(HttpStatusCode.OK);
        var sent = new List<string?>();
        var withHeaders = new StubHandler(request =>
        {
            sent.Add(request.Headers.TryGetValues(ApiHeaders.RequestedWith, out var v) ? v.Single() : null);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        api = new AccountApi(new HttpClient(withHeaders) { BaseAddress = new Uri("http://localhost/") });

        await api.LoginAsync(new LoginBody("a@x.test", "pw"));
        await api.LogoutAsync();
        await api.DeleteAccountAsync();

        Assert.That(sent,
            Is.EqualTo(new[]
            {
                ApiHeaders.RequestedWithWeb, ApiHeaders.RequestedWithWeb, ApiHeaders.RequestedWithWeb
            }));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task Login_PostsTheBody_AndAsksForTheCookie()
    {
        var (api, handler) = Api(HttpStatusCode.OK);

        await api.LoginAsync(new LoginBody("a@x.test", "pw"));

        var request = handler.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(request.Url, Is.EqualTo("http://localhost/account/login?useCookies=true"));
            Assert.That(request.Body,
                Is.EqualTo(
                    """{"email":"a@x.test","password":"pw","twoFactorCode":null,"twoFactorRecoveryCode":null}"""));
        });
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.NotFound)]
    public async Task Me_IsNull_ForAGuest_OrAnInstanceWithoutAccounts(HttpStatusCode status)
    {
        var (api, _) = Api(status);

        Assert.That(await api.MeAsync(), Is.Null);
    }

    [Test]
    public async Task Me_IsThePerson_WhenSignedIn()
    {
        var (api, _) = Api(HttpStatusCode.OK,
            """{"userId":"1","tenantId":"home","email":"a@x.test","displayName":"Ann","emailConfirmed":true,"logins":[],"hasPassword":true}""");

        var me = await api.MeAsync();

        // A record with a list compares the list by reference, so the fields are read one by one.
        Assert.Multiple(() =>
        {
            Assert.That((me!.UserId, me.TenantId, me.Email, me.DisplayName, me.EmailConfirmed, me.HasPassword),
                Is.EqualTo(("1", "home", "a@x.test", "Ann", true, true)));
            Assert.That(me.Logins, Is.Empty);
        });
    }

    [Test]
    public void ARefusedSignIn_ThrowsWithTheCode()
    {
        var (api, _) = Api(HttpStatusCode.Unauthorized, """{"status":401,"detail":"LockedOut"}""");

        var refused = Assert.ThrowsAsync<AccountApiException>(() => api.LoginAsync(new LoginBody("a@x.test", "pw")));

        Assert.Multiple(() =>
        {
            Assert.That(refused!.Status, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(refused.Problem.Detail, Is.EqualTo("LockedOut"));
        });
    }

    [Test]
    public async Task Enabled_IsWhetherTheProvidersRouteAnswers()
    {
        var (on, _) = Api(HttpStatusCode.OK, """{"providers":[]}""");
        var (off, _) = Api(HttpStatusCode.NotFound);

        Assert.Multiple(async () =>
        {
            Assert.That(await on.EnabledAsync(), Is.True);
            Assert.That(await off.EnabledAsync(), Is.False);
        });
    }
}

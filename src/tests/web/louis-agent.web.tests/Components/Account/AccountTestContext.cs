using System.Net;
using System.Text;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Account;

/// <summary>
/// A bUnit context with what the account components ask for, over an API the test scripts: each call answers by its
/// address, and every request is kept so a test can read what was sent.
/// </summary>
internal static class AccountTestContext
{
    /// <summary>A context whose API answers every address through <paramref name="answer"/>; English words.</summary>
    public static (BunitContext Context, StubHandler Handler) New(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(answer);
        ctx.Services.AddSingleton(
            new AccountApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        ctx.Services.AddSingleton(sp => new AccountState(sp.GetRequiredService<AccountApi>()));
        ctx.Services.AddSingleton(new AccountText());
        return (ctx, handler);
    }

    /// <summary>A context whose API answers everything with this status and body.</summary>
    public static (BunitContext Context, StubHandler Handler) New(HttpStatusCode status, string json = "") =>
        New(_ => Reply(status, json));

    /// <summary>One reply.</summary>
    public static HttpResponseMessage Reply(HttpStatusCode status, string json = "") => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    /// <summary>The JSON of a signed-in Ann, as /account/me answers.</summary>
    public const string Ann =
        """{"userId":"1","tenantId":"home","email":"ann@x.test","displayName":"Ann","emailConfirmed":true,"logins":[],"hasPassword":true}""";
}

using System.Net;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Components.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Components.Profile;

/// <summary>Linked providers can be unlinked; providers with keys that aren't linked yet get a link; a refusal says why.</summary>
public class LinkedLoginsTests
{
    private const string Providers =
        """{"providers":[{"name":"google","label":"Google","enabled":true},{"name":"microsoft","label":"Microsoft","enabled":true},{"name":"linkedin","label":"LinkedIn","enabled":false}]}""";

    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", "Ann", true, ["google"], true);

    [Test]
    public void TheLinkedOnes_HaveUnlink_AndTheEnabledOthers_HaveLink()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/providers", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, Providers)
                : AccountTestContext.Reply(HttpStatusCode.NoContent));
        using var __ = ctx;

        var cut = ctx.Render<LinkedLogins>(p => p.Add(l => l.Me, Ann));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find(".account-logins li span").TextContent, Is.EqualTo("Google"));
            Assert.That(cut.Find(".account-logins button").TextContent, Is.EqualTo("Unlink"));
            var links = cut.FindAll(".account-providers a");
            Assert.That(links.Select(a => a.TextContent), Is.EqualTo(new[] { "Link Microsoft" }),
                "LinkedIn has no keys");
            Assert.That(links[0].GetAttribute("href"),
                Is.EqualTo("account/external/microsoft?link=true&returnUrl=profile"));
        });
    }

    [Test]
    public void Unlink_Calls_AndTheStateTakesTheAccountAfter()
    {
        var (ctx, handler) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/providers", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, Providers)
                : request.RequestUri.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                    ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                    : AccountTestContext.Reply(HttpStatusCode.NoContent));
        using var _ = ctx;
        var cut = ctx.Render<LinkedLogins>(p => p.Add(l => l.Me, Ann));
        cut.WaitForElement(".account-logins button");

        cut.Find(".account-logins button").Click();

        cut.WaitForAssertion(() => Assert.That(ctx.Services.GetRequiredService<AccountState>().Me?.Logins, Is.Empty));
        Assert.That(handler.Requests.Select(r => $"{r.Method} {r.Url}"),
            Does.Contain("DELETE http://localhost/account/external/google"));
    }

    [Test]
    public void ARefusedUnlink_SaysWhy()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/providers", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, Providers)
                : AccountTestContext.Reply(HttpStatusCode.BadRequest,
                    """{"status":400,"errors":{"provider":["This is the last way into the account: set a password first."]}}"""));
        using var __ = ctx;
        var cut = ctx.Render<LinkedLogins>(p => p.Add(l => l.Me, Ann with { HasPassword = false }));
        cut.WaitForElement(".account-logins button");

        cut.Find(".account-logins button").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".account-notice li").TextContent, Does.Contain("last way into")));
    }
}

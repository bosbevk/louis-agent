using System.Net;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Components.Account;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Account;

/// <summary>The sign-in form sends the two fields, tells the page when it's in, and says why when it's refused.</summary>
public class LoginFormTests
{
    [Test]
    public void ASignIn_PostsTheFields_ReadsWhoSignedIn_AndTellsThePage()
    {
        var (ctx, handler) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.OK));
        using var _ = ctx;
        bool signedIn = false;
        var cut = ctx.Render<LoginForm>(p => p.Add(form => form.OnSignedIn, () => signedIn = true));

        cut.Find("input[type=email]").Change("ann@x.test");
        cut.Find("input[type=password]").Input("Long-enough-1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(signedIn, Is.True));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests[0].Url, Is.EqualTo("http://localhost/account/login?useCookies=true"));
            Assert.That(handler.Requests[0].Body,
                Does.Contain("\"email\":\"ann@x.test\"").And.Contain("\"password\":\"Long-enough-1\""));
            Assert.That(ctx.Services.GetRequiredService<AccountState>().Me?.Email, Is.EqualTo("ann@x.test"));
        });
    }

    [Test]
    public void ARefusal_SaysWhy_InTheSitesWords()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.Unauthorized, """{"status":401,"detail":"LockedOut"}""");
        using var __ = ctx;
        var cut = ctx.Render<LoginForm>();

        cut.Find("input[type=email]").Change("ann@x.test");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice.is-error").TextContent,
            Does.Contain("Too many tries")));
    }

    [Test]
    public void ASecondFactor_OpensTheCodeBox_AndSendsAgainWithTheCode()
    {
        var (ctx, handler) = AccountTestContext.New(request => SecondFactor(request));
        using var _ = ctx;
        bool signedIn = false;
        var cut = ctx.Render<LoginForm>(p => p.Add(form => form.OnSignedIn, () => signedIn = true));
        cut.Find("input[type=email]").Change("ann@x.test");
        cut.Find("input[type=password]").Input("Long-enough-1");

        cut.Find("form").Submit();
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".account-notice:not(.is-error)").TextContent, Does.Contain("authenticator app")));
        cut.Find("input[inputmode=numeric]").Change("123456");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(signedIn, Is.True));
        Assert.That(handler.Requests[1].Body, Does.Contain("\"twoFactorCode\":\"123456\""));
    }

    /// <summary>The API with two-factor on: the password alone is 401 RequiresTwoFactor, with the code it's in.</summary>
    private static HttpResponseMessage SecondFactor(HttpRequestMessage request)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann);
        string body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
        if (body.Contains("\"twoFactorCode\":\"123456\"", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK);
        return AccountTestContext.Reply(HttpStatusCode.Unauthorized, """{"status":401,"detail":"RequiresTwoFactor"}""");
    }

    [Test]
    public void TheForm_LinksToRegisterAndReset()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK);
        using var __ = ctx;

        var cut = ctx.Render<LoginForm>();

        Assert.That(cut.FindAll(".account-links a").Select(a => a.GetAttribute("href")),
            Is.EqualTo(new[] { "forgot-password", "register" }));
    }
}

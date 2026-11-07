using System.Net;
using LouisAgent.Web.Pages.Account;
using LouisAgent.Web.Tests.Components.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Account;

/// <summary>The confirmation landing says what happened; forgot mails a code; reset sets the password with it.</summary>
public class ConfirmAndResetPagesTests
{
    [TestCase(true, "Your email is confirmed")]
    [TestCase(false, "That link didn't work")]
    public void TheConfirmationLanding_SaysWhatHappened(bool done, string expected)
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK);
        using var __ = ctx;
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("done", done));

        var cut = ctx.Render<ConfirmEmailPage>();

        Assert.That(cut.Find(".account-notice").TextContent, Does.Contain(expected));
    }

    [Test]
    public void Forgot_PostsTheEmail_AndPointsAtTheResetPage_WithIt()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var cut = ctx.Render<ForgotPasswordPage>();

        cut.Find("input[type=email]").Change("ann@x.test");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("a code is on its way")));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Single().Url, Is.EqualTo("http://localhost/account/forgotPassword"));
            Assert.That(cut.Find("a.account-button").GetAttribute("href"),
                Is.EqualTo("reset-password?email=ann%40x.test"));
        });
    }

    [Test]
    public void Reset_TakesTheEmailFromTheQuery_AndPostsTheCodeAndPassword()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("email", "ann@x.test"));
        var cut = ctx.Render<ResetPasswordPage>();

        cut.Find("input[type=text]").Change("123456");
        foreach (var password in cut.FindAll("input[type=password]")) password.Input("Long-enough-1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("Your password is set")));
        Assert.That(handler.Requests.Single().Body,
            Is.EqualTo("""{"email":"ann@x.test","resetCode":"123456","newPassword":"Long-enough-1"}"""));
    }

    [Test]
    public void Reset_RefusesPasswordsThatDiffer_BeforeAnyCall()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var cut = ctx.Render<ResetPasswordPage>();

        cut.Find("input[type=email]").Change("ann@x.test");
        cut.Find("input[type=text]").Change("123456");
        var passwords = cut.FindAll("input[type=password]");
        passwords[0].Input("Long-enough-1");
        passwords[1].Input("Other-2");
        cut.Find("form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".account-notice.is-error").TextContent, Does.Contain("aren't the same"));
            Assert.That(handler.Requests, Is.Empty);
        });
    }
}

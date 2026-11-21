using System.Net;
using LouisAgent.Web.Accounts.Pages;
using LouisAgent.Web.Accounts.Tests.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Pages;

/// <summary>The sign-in page draws the form for a guest, and sends a signed-in person on, only ever to one of our pages.</summary>
public class LoginPageTests
{
    [Test]
    public void AGuest_SeesTheForm_InEnglish()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.Unauthorized)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;

        var cut = ctx.Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Sign in"));
            Assert.That(cut.FindAll("form.account-form"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SomeoneSignedIn_IsSentOn_ToOneOfOurPages()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("returnUrl", "//evil.test"));

        var cut = ctx.Render<LoginPage>();

        // Home, not evil.test: the query's address is checked before it's followed.
        cut.WaitForAssertion(() => Assert.That(navigation.Uri, Is.EqualTo("http://localhost/")));
    }
}

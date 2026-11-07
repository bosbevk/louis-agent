using System.Net;
using LouisAgent.Web.Pages.Account;
using LouisAgent.Web.Tests.Components.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Account;

/// <summary>The profile draws the signed-in person's sections, and sends a guest to sign in with the way back.</summary>
public class ProfilePageTests
{
    [Test]
    public void ASignedInPerson_SeesEverySection()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;

        var cut = ctx.Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Your profile"));
            Assert.That(cut.FindAll(".account-section"), Has.Count.EqualTo(6),
                "name, email, password, logins, delete, and the way out");
            Assert.That(cut.FindAll("button").Last().TextContent, Is.EqualTo("Sign out"));
        });
    }

    [Test]
    public void AGuest_IsSentToSignIn_WithTheWayBack()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.Unauthorized)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();

        var cut = ctx.Render<ProfilePage>();

        cut.WaitForAssertion(() => Assert.That(navigation.Uri, Is.EqualTo("http://localhost/login?returnUrl=profile")));
    }
}

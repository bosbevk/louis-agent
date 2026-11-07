using System.Net;
using LouisAgent.Web.Accounts.Pages;
using LouisAgent.Web.Accounts.Tests.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Pages;

/// <summary>The profile draws the signed-in person's sections, and sends a guest to sign in with the way back.</summary>
public class ProfilePageTests
{
    /// <summary>What the profile's sections read, each at rest: no passkeys yet, two-factor off, no providers set up.</summary>
    private static HttpResponseMessage Empty(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/passkeys", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK, """{"passkeys":[]}""");
        if (path.EndsWith("/2fa", StringComparison.Ordinal))
            return AccountTestContext.Reply(HttpStatusCode.OK,
                """{"sharedKey":"KEY","recoveryCodesLeft":0,"isTwoFactorEnabled":false,"isMachineRemembered":false}""");
        return AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}""");
    }

    [Test]
    public void ASignedInPerson_SeesEverySection()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : Empty(request));
        using var __ = ctx;

        var cut = ctx.Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Your profile"));
            Assert.That(cut.FindAll(".account-section"), Has.Count.EqualTo(8),
                "name, email, password, logins, passkeys, two-factor, delete, and the way out");
            Assert.That(cut.FindAll("button").Last().TextContent, Is.EqualTo("Sign out"));
        });
    }

    [Test]
    public void AGuest_IsSentToSignIn_WithTheWayBack()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.Unauthorized)
                : Empty(request));
        using var __ = ctx;
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();

        var cut = ctx.Render<ProfilePage>();

        cut.WaitForAssertion(() => Assert.That(navigation.Uri, Is.EqualTo("http://localhost/login?returnUrl=profile")));
    }
}

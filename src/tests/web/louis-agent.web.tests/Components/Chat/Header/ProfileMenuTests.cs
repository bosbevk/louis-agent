using System.Net;
using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Tests.Components.Account;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Chat.Header;

/// <summary>
/// The profile's menu: a guest on a site with accounts gets sign in and register; a signed-in person gets their name,
/// their profile and sign out; a site with no account services at all gets the guest's menu as it always was.
/// </summary>
public class ProfileMenuTests
{
    [Test]
    public void WithNoAccountServices_ItIsTheGuestsMenu_AsBefore()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ProfileMenu>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Guest"));
            Assert.That(cut.FindAll("a[href=login]"), Is.Empty);
            Assert.That(cut.FindAll("button.dropdown-item").Select(b => b.TextContent.Trim()),
                Does.Contain("Profile").And.Contain("Sign out"));
        });
    }

    [Test]
    public void AGuest_OnASiteWithAccounts_GetsSignInAndRegister()
    {
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.Unauthorized)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ProfileMenu>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find("a[href=login]").TextContent.Trim(), Is.EqualTo("Sign in"));
            Assert.That(cut.Find("a[href=register]").TextContent.Trim(), Is.EqualTo("Create an account"));
            Assert.That(cut.FindAll("button.dropdown-item").Select(b => b.TextContent.Trim()),
                Does.Not.Contain("Sign out"));
        });
    }

    [Test]
    public void ASignedInPerson_SeesTheirName_TheirProfile_AndSignOut()
    {
        var (ctx, handler) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.NoContent, """{"providers":[]}"""));
        using var _ = ctx;
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ProfileMenu>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Find(".profile-button").TextContent, Is.EqualTo("A"));
            Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Ann"));
            Assert.That(cut.Find(".profile-who small").TextContent, Is.EqualTo("ann@x.test"));
            Assert.That(cut.Find("a[href=profile]").TextContent.Trim(), Is.EqualTo("Profile"));
        });
        cut.FindAll("button.dropdown-item").Single(b => b.TextContent.Contains("Sign out")).Click();
        cut.WaitForAssertion(() =>
            Assert.That(handler.Requests.Select(r => r.Url), Does.Contain("http://localhost/account/logout")));
    }
}

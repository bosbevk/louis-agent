using System.Net;
using LouisAgent.Web.Accounts.Menu;
using LouisAgent.Web.Components.Chat.Header.Menu;
using LouisAgent.Web.Tests.Components.Account;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Accounts.Menu;

/// <summary>The seam's real side: who the API says is here, the site's words, and sign out through the API.</summary>
public class AccountMenuTests
{
    [Test]
    public async Task ASignedInPerson_IsNamed_AndSignsOutAtTheApi()
    {
        var (ctx, handler) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.OK, AccountTestContext.Ann)
                : AccountTestContext.Reply(HttpStatusCode.NoContent));
        using var _ = ctx;
        var menu = ctx.Services.GetRequiredService<IAccountMenu>();
        bool changed = false;
        menu.Changed += () => changed = true;

        await menu.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(menu, Is.InstanceOf<AccountMenu>());
            Assert.That(menu.Enabled, Is.True);
            Assert.That(menu.SignedIn, Is.True);
            Assert.That(menu.Name, Is.EqualTo("Ann"));
            Assert.That(menu.Email, Is.EqualTo("ann@x.test"));
            Assert.That(menu.SignInLabel, Is.EqualTo("Sign in"));
            Assert.That(changed, Is.True);
        });

        await menu.SignOutAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Select(r => r.Url), Does.Contain("http://localhost/account/logout"));
            Assert.That(menu.SignedIn, Is.False);
        });
    }

    [Test]
    public async Task AGuest_IsNobody_OnASiteWithAccounts()
    {
        // Nobody is signed in, but the API has accounts: the providers' list answers.
        var (ctx, _) = AccountTestContext.New(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/me", StringComparison.Ordinal)
                ? AccountTestContext.Reply(HttpStatusCode.Unauthorized)
                : AccountTestContext.Reply(HttpStatusCode.OK, """{"providers":[]}"""));
        using var __ = ctx;
        var menu = ctx.Services.GetRequiredService<IAccountMenu>();

        await menu.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(menu.Enabled, Is.True);
            Assert.That(menu.SignedIn, Is.False);
            Assert.That(menu.Name, Is.Null);
        });
    }
}

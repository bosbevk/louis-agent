using System.Net;
using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Components.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Components.Profile;

/// <summary>The profile's sections: the name saves and the state follows; the password and email go to Identity's info route.</summary>
public class ProfileSectionsTests
{
    private static readonly AccountInfo Ann = new("1", "home", "ann@x.test", null, true, [], true);

    [Test]
    public void TheDetails_ShowTheEmail_AndSaveTheName_IntoTheState()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK, AccountTestContext.Ann);
        using var _ = ctx;
        var cut = ctx.Render<ProfileDetails>(p => p.Add(d => d.Me, Ann));

        Assert.That(cut.Find(".account-facts dd").TextContent, Does.Contain("ann@x.test").And.Contain("confirmed"));
        cut.Find("input[type=text]").Change("Ann");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice").TextContent, Is.EqualTo("Saved.")));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Single().Body, Is.EqualTo("""{"displayName":"Ann"}"""));
            Assert.That(ctx.Services.GetRequiredService<AccountState>().Me?.DisplayName, Is.EqualTo("Ann"));
        });
    }

    [Test]
    public void APasswordChange_SendsBoth_ToTheInfoRoute()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var cut = ctx.Render<ChangePassword>(p => p.Add(c => c.Me, Ann));

        var fields = cut.FindAll("input[type=password]");
        fields[0].Input("Old-pass-1");
        fields[1].Input("New-pass-2");
        fields[2].Input("New-pass-2");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("changed")));
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Single().Url, Is.EqualTo("http://localhost/account/manage/info"));
            Assert.That(handler.Requests.Single().Body,
                Is.EqualTo("""{"newEmail":null,"newPassword":"New-pass-2","oldPassword":"Old-pass-1"}"""));
        });
    }

    [Test]
    public void AnAccountWithoutAPassword_HasNoPasswordSection()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK);
        using var __ = ctx;

        var cut = ctx.Render<ChangePassword>(p => p.Add(c => c.Me, Ann with { HasPassword = false }));

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void AnEmailChange_IsMailedToTheNewAddress()
    {
        var (ctx, handler) = AccountTestContext.New(HttpStatusCode.OK);
        using var _ = ctx;
        var cut = ctx.Render<ChangeEmail>();

        cut.Find("input[type=email]").Change("new@x.test");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".account-notice").TextContent, Does.Contain("new@x.test")));
        Assert.That(handler.Requests.Single().Body, Does.Contain("\"newEmail\":\"new@x.test\""));
    }
}

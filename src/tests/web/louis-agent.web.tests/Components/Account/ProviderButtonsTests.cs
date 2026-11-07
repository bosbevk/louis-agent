using System.Net;
using LouisAgent.Web.Components.Account;

namespace LouisAgent.Web.Tests.Components.Account;

/// <summary>A button per provider the instance has keys for, each leading to the API's challenge route; none draws nothing.</summary>
public class ProviderButtonsTests
{
    [Test]
    public void NoProvider_DrawsNothing()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK, """{"providers":[]}""");
        using var __ = ctx;

        var cut = ctx.Render<ProviderButtons>();

        cut.WaitForAssertion(() => Assert.That(cut.Markup.Trim(), Is.Empty));
    }

    [Test]
    public void AnEnabledProvider_IsAButton_ToTheChallengeRoute_WithASafeReturn()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK,
            """{"providers":[{"name":"google","label":"Google","enabled":true},{"name":"x","label":"X","enabled":false}]}""");
        using var __ = ctx;

        var cut = ctx.Render<ProviderButtons>(p => p.Add(b => b.ReturnUrl, "//evil.test"));

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll(".account-providers a");
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links[0].TextContent, Is.EqualTo("Sign in with Google"));
            Assert.That(links[0].GetAttribute("href"), Is.EqualTo("account/external/google?returnUrl=.%2F"));
        });
    }
}

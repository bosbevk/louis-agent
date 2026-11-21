using System.Net;
using LouisAgent.Web.Accounts.Components;

namespace LouisAgent.Web.Accounts.Tests.Components.Providers;

/// <summary>
/// Each provider's button wears that provider's own look (FX67, Louis: "google have their own button look and feel, we
/// must comply"): its logo, unchanged, beside the words, in its own class; a provider we have no brand for keeps the
/// plain button.
/// </summary>
public class ProviderBrandTests
{
    private const string AllThree =
        """{"providers":[{"name":"google","label":"Google","enabled":true},{"name":"microsoft","label":"Microsoft","enabled":true},{"name":"linkedin","label":"LinkedIn","enabled":true}]}""";

    [TestCase("google", "is-google", "Sign in with Google")]
    [TestCase("microsoft", "is-microsoft", "Sign in with Microsoft")]
    [TestCase("linkedin", "is-linkedin", "Sign in with LinkedIn")]
    public void AKnownProvider_WearsItsOwnLook_WithItsLogo(string name, string look, string words)
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK, AllThree);
        using var __ = ctx;

        var cut = ctx.Render<ProviderButtons>();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find($"a[href^='account/external/{name}']");
            Assert.That(button.ClassList, Does.Contain("provider-button").And.Contain(look));
            Assert.That(button.ClassList, Does.Not.Contain("account-button"), "the site's own action look stays off it");
            Assert.That(button.QuerySelector("svg.provider-logo[aria-hidden='true']"), Is.Not.Null, "the logo, beside the words");
            Assert.That(button.TextContent.Trim(), Is.EqualTo(words));
        });
    }

    [Test]
    public void GooglesLogo_IsTheFourColourG()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK, AllThree);
        using var __ = ctx;

        var cut = ctx.Render<ProviderButtons>();

        cut.WaitForAssertion(() => Assert.That(
            cut.FindAll(".is-google svg path").Select(path => path.GetAttribute("fill")),
            Is.EqualTo(new[] { "#EA4335", "#4285F4", "#FBBC05", "#34A853" })));
    }

    [Test]
    public void AProviderWithNoBrand_KeepsThePlainButton()
    {
        var (ctx, _) = AccountTestContext.New(HttpStatusCode.OK,
            """{"providers":[{"name":"github","label":"GitHub","enabled":true}]}""");
        using var __ = ctx;

        var cut = ctx.Render<ProviderButtons>();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find(".account-providers a");
            Assert.That(button.ClassList, Does.Contain("account-button").And.Contain("is-plain"));
            Assert.That(button.QuerySelector("svg"), Is.Null);
        });
    }
}

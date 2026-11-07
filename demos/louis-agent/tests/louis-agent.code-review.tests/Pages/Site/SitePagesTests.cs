using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.CodeReview.Pages.Site.Framework;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site;

/// <summary>
/// The site's pages, now pages of this app (F109.1): each answers at its address with its heading, and says the
/// framework's structure as it stands, not the one from before the split.
/// </summary>
public class SitePagesTests
{
    /// <summary>Each page of the menu is at its address and opens with its heading.</summary>
    [TestCase(typeof(HomePage), "/", "A turn goes down the stack and comes back priced.")]
    [TestCase(typeof(HowItWorksPage), "/how-it-works", "One turn, through every layer.")]
    [TestCase(typeof(PacksPage), "/packs", "What an agent can do is a package.")]
    [TestCase(typeof(BuildYourOwnPage), "/build-your-own", "A few lines of your own.")]
    [TestCase(typeof(ProofPage), "/proof", "Every number here comes from the ledger or an eval run.")]
    [TestCase(typeof(BuiltOnItPage), "/built-on-it", "Two agents run on the engine today.")]
    [TestCase(typeof(ContactPage), "/contact", "Building an agent for a .NET team?")]
    public void APage_IsAtItsAddress_WithItsHeading(Type page, string address, string heading)
    {
        using var ctx = new BunitContext();

        var routes = page.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Select(route => route.Template);
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.Multiple(() =>
        {
            Assert.That(routes, Is.EqualTo(new[] { address }));
            Assert.That(cut.Find("h2.chrome").TextContent, Is.EqualTo(heading));
        });
    }

    /// <summary>The words from before the split are gone: a shops pack, tools in the engine.</summary>
    [TestCase(typeof(BuiltOnItPage), "A shops pack")]
    [TestCase(typeof(HowItWorksPage), "tools sorted by name")]
    [TestCase(typeof(HowItWorksPage), "The tool loop, streaming, the providers, the usage ledger and the guards.")]
    [TestCase(typeof(HomePage), "60+ ADRs")]
    public void APage_DropsTheOldStructure(Type page, string old)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.Markup, Does.Not.Contain(old));
    }

    /// <summary>What's new is on the page: a demo is built on the framework, a pack never needs the engine.</summary>
    [TestCase(typeof(HomePage), "An AI agent framework")]
    [TestCase(typeof(HowItWorksPage), "holds no agent and no tools")]
    [TestCase(typeof(HowItWorksPage), "never the engine")]
    [TestCase(typeof(BuiltOnItPage), "Each is a demo")]
    [TestCase(typeof(PacksPage), "never the engine")]
    public void APage_TellsTheNewStructure(Type page, string words)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.Markup, Does.Contain(words));
    }

    /// <summary>The landing shows the short film; How it works the detailed one; neither the other's.</summary>
    [TestCase(typeof(HomePage), "film-short", "film-detail")]
    [TestCase(typeof(HowItWorksPage), "film-detail", "film-short")]
    public void APage_ShowsItsFilm(Type page, string film, string notThisOne)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.FindAll($".stage.{film}"), Has.Count.EqualTo(1));
        Assert.That(cut.Markup, Does.Not.Contain(notThisOne));
    }
}

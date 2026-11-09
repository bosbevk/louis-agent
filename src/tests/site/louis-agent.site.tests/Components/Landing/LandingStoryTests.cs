using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Components.Landing;

/// <summary>
/// The landing tells its story offer first, the framework as the proof (F128.1, Louis's picks 2A and 7A): the offer,
/// what the agent does, how it works for you and why you can trust it, custom agents, the demos, then the framework's
/// own sections, and a last strip that speaks to a business.
/// </summary>
public class LandingStoryTests
{
    /// <summary>The sections, in the order a buyer meets them.</summary>
    [Test]
    public void TheLanding_TellsItsStory_OfferFirst()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();

        Assert.That(cut.FindAll("section").Select(section => section.Id),
            Is.EqualTo(new[]
            {
                "top", "does", "for-you", "custom", "demos", "rule", "proof", "how", "concepts", "built", "contact"
            }));
    }

    /// <summary>How it works for you is three steps, then the facts a buyer trusts.</summary>
    [Test]
    public void HowItWorksForYou_IsThreeSteps_AndTheFactsABuyerTrusts()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#for-you");

        Assert.Multiple(() =>
        {
            Assert.That(band.QuerySelectorAll("ol.buyer-steps > li"), Has.Length.EqualTo(3));
            Assert.That(band.TextContent, Does.Contain("short call").And.Contain("made-up data"));
            Assert.That(band.TextContent, Does.Contain("South Africa").And.Contain("measured")
                .And.Contain("closed").And.Contain("what you give it"));
        });
    }

    /// <summary>The demos band leads to the demos.</summary>
    [Test]
    public void TheDemosBand_LeadsToTheDemos()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#demos");

        Assert.That(band.QuerySelectorAll("a[href='/demos']"), Has.Length.GreaterThanOrEqualTo(1));
    }

    /// <summary>The last strip speaks to a business and asks for theirs.</summary>
    [Test]
    public void TheLastStrip_SpeaksToABusiness()
    {
        using var ctx = new BunitContext();

        var strip = ctx.Render<HomePage>().Find("section#contact");

        Assert.Multiple(() =>
        {
            Assert.That(strip.TextContent, Does.Contain("business"));
            Assert.That(strip.TextContent, Does.Not.Contain("NuGet"));
            Assert.That(strip.QuerySelector("a[href='/contact']")!.TextContent, Is.EqualTo("Ask for yours"));
        });
    }
}

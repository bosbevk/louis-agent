using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.Framework;
using LouisAgent.Site.Tests.Pages;

namespace LouisAgent.Site.Tests.Components.Landing;

/// <summary>
/// The landing tells its story offer first, the framework as the proof (F128.1, Louis's picks 2A and 7A): the offer,
/// what the agent does, how it works for you and why you can trust it, custom agents, the demos, then the framework's
/// own sections, and a last strip that speaks to a business.
/// </summary>
public class LandingStoryTests
{
    /// <summary>
    /// The sections, in the order a buyer meets them, and the framework as one band of proof (Louis: "I thought we will
    /// rework it and move framework stuff out"): its rule, film, concepts and what's built on it live on its own pages.
    /// </summary>
    [Test]
    public void TheLanding_TellsItsStory_OfferFirst()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();

        Assert.That(cut.FindAll("section").Select(section => section.Id),
            Is.EqualTo(new[] { "top", "does", "for-you", "custom", "demos", "proof", "contact" }));
    }

    /// <summary>The proof band says what the site stands on and leads into the framework's own pages.</summary>
    [TestCase("/framework/how-it-works")]
    [TestCase("/framework/architecture")]
    [TestCase("/framework/measured")]
    [TestCase("/framework/built-on-it")]
    public void TheProofBand_LeadsIntoTheFramework(string address)
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#proof");

        Assert.Multiple(() =>
        {
            Assert.That(band.TextContent, Does.Contain("framework of its own"));
            Assert.That(band.QuerySelectorAll($"a[href='{address}']"), Has.Length.EqualTo(1));
        });
    }

    /// <summary>What left the landing has a home on the framework's pages.</summary>
    [Test]
    public void WhatLeftTheLanding_LivesOnTheFrameworksPages()
    {
        using var ctx = SitePageContext.Create();

        string measured = ctx.Render<ProofPage>().Markup;
        string architecture = ctx.Render<ArchitecturePage>().Markup;

        Assert.Multiple(() =>
        {
            Assert.That(measured, Does.Contain("First principles."));
            Assert.That(architecture, Does.Contain("film-short").And.Contain("The engine is how"));
        });
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

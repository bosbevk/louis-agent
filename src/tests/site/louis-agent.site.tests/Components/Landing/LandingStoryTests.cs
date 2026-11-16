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
            Is.EqualTo(new[] { "top", "does", "for-you", "demos", "custom", "contact" }));
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
            Assert.That(architecture, Does.Contain("The engine is how"));
        });
    }

    /// <summary>
    /// Where it runs, as the design page has it (FX162): your server, your model and your data, three cards.
    /// </summary>
    [Test]
    public void WhereItRuns_IsYourServerYourModelAndYourData()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#for-you");

        Assert.Multiple(() =>
        {
            Assert.That(band.QuerySelector("h2")!.TextContent, Is.EqualTo("It runs where you say."));
            Assert.That(band.QuerySelectorAll(".tiles article h3").Select(title => title.TextContent),
                Is.EqualTo(new[] { "Your server", "Your model", "Your data" }));
        });
    }

    /// <summary>
    /// The demos band fans out the three businesses, each a way into its demo (FX162, the design's fan).
    /// </summary>
    [Test]
    public void TheDemosBand_LeadsToEachDemo()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#demos");

        Assert.That(band.QuerySelectorAll(".shot-fan a").Select(shot => shot.GetAttribute("href")),
            Is.EqualTo(new[] { "/law-practice-demo", "/guesthouse-demo", "/restaurant-demo" }));
    }

    /// <summary>
    /// The last strip speaks to a business with no price on it (Louis: "a request to the ai to take their details and
    /// a link to our contact page, I do quotes on request"): a button that asks the agent to take the visitor's
    /// details, and a link to the contact page.
    /// </summary>
    [Test]
    public void TheLastStrip_AsksTheAgent_OrLeadsToContact()
    {
        using var ctx = new BunitContext();

        var strip = ctx.Render<HomePage>().Find("section#contact");

        Assert.Multiple(() =>
        {
            Assert.That(strip.TextContent, Does.Contain("business").And.Contain("on request"));
            Assert.That(strip.TextContent, Does.Not.Contain("R500").And.Not.Contain("Louis"));
            Assert.That(strip.QuerySelector("button[data-ask]")!.GetAttribute("data-ask"),
                Does.Contain("take my details"));
            Assert.That(strip.QuerySelector("a[href='/contact']")!.TextContent, Is.EqualTo("Contact us"));
        });
    }
}

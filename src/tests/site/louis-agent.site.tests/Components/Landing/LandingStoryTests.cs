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
            Is.EqualTo(new[] { "top", "does", "for-you", "brand", "demos", "custom", "who", "contact" }));
    }

    /// <summary>
    /// The landing shows who builds it, once, with his face and his name (Louis's pick, 2026-11-18, "Face and name,
    /// once"): a small owner buys from a person, and the research found no rival shows one. It leads to his CV.
    /// </summary>
    [Test]
    public void TheLanding_ShowsWhoBuildsIt_WithHisFace()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#who");

        var cv = band.QuerySelectorAll("a").Single(link => link.GetAttribute("href") == "/cv");
        Assert.Multiple(() =>
        {
            Assert.That(band.QuerySelector("h2")!.TextContent, Is.EqualTo("Built by Louis Marais in Malmesbury."));
            Assert.That(band.QuerySelector("img.profile-photo")!.GetAttribute("src"), Is.EqualTo("images/louis-marais.jpg"));
            Assert.That(band.QuerySelector("img.profile-photo")!.GetAttribute("alt"), Is.EqualTo("Louis Marais"));
            Assert.That(cv.TextContent.Trim(), Is.EqualTo("Read his CV"));
            Assert.That(band.TextContent, Does.Not.Contain(":").And.Not.Contain(";"));
        });
    }

    /// <summary>
    /// Two free websites, told in one line under the hero (Louis, 2026-11-18: "why not just have this and link to
    /// page?", then his pick of a framework strip): the whole line leads to the offer's own page.
    /// </summary>
    [Test]
    public void TheFreeWebsites_AreOneLineUnderTheHero_LeadingToTheirPage()
    {
        using var ctx = new BunitContext();

        var strip = ctx.Render<HomePage>().Find("section#top a.announcement-strip");

        Assert.Multiple(() =>
        {
            Assert.That(strip.GetAttribute("href"), Is.EqualTo("/free-websites"));
            Assert.That(strip.QuerySelector(".status-pill")!.TextContent, Is.EqualTo("Entries open"));
            Assert.That(strip.QuerySelector(".announcement-text")!.TextContent,
                Is.EqualTo("Two businesses get a free website."));
            Assert.That(strip.QuerySelector(".announcement-go")!.TextContent, Is.EqualTo("See how it works"));
        });
    }

    /// <summary>
    /// FX169: a section for the brand in the landing's middle (Louis: "add a slide dedicated to branding mixed with
    /// one random topic", "brand section must be in the middle"): the idea, the mark large with the name on four
    /// grounds, and the theme as its other topic, leading to the theme's page. None of its panels can be picked to
    /// ask the guide about (Louis: "thise panels must not be clickable to the ai to explain").
    /// </summary>
    [Test]
    public void TheBrand_HasASectionOfItsOwn_WithTheTheme_AndNothingToPick()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#brand");

        Assert.Multiple(() =>
        {
            Assert.That(band.QuerySelector(".mark-showcase .mark-grounds"), Is.Not.Null);
            Assert.That(band.QuerySelectorAll("[data-piece]"), Is.Empty);
            Assert.That(band.QuerySelector("a[href='/framework/theme']"), Is.Not.Null);
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
            Assert.That(architecture, Does.Contain("The engine is how"));
        });
    }

    /// <summary>
    /// Where it runs, for an owner (the rewrite of 2026-11-18): we host it, or a bigger business keeps it on its own
    /// servers, and the data stays where it is put, three cards.
    /// </summary>
    [Test]
    public void WhereItRuns_IsHostedHere_OrOnYourOwnServers()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<HomePage>().Find("section#for-you");

        Assert.Multiple(() =>
        {
            Assert.That(band.QuerySelector("h2")!.TextContent, Is.EqualTo("Your data stays in South Africa."));
            Assert.That(band.QuerySelectorAll(".tiles article h3").Select(title => title.TextContent),
                Is.EqualTo(new[] { "We host it", "Or on your own servers", "Your data" }));
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
            Assert.That(strip.TextContent, Does.Contain("business").And.Contain("after a short call"));
            Assert.That(strip.TextContent, Does.Not.Contain("R500").And.Not.Contain("Louis"));
            Assert.That(strip.QuerySelector("button.btn")!.TextContent, Is.EqualTo("Leave your details"));
            Assert.That(strip.QuerySelector("a[href='/contact']")!.TextContent, Is.EqualTo("Write to us"));
        });
    }
}

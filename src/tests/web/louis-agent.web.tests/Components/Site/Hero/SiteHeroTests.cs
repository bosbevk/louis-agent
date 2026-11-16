using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Hero;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Hero;

/// <summary>
/// A site's hero is the framework's (F118, ADR 0106): the topic, the headline, the summary, the note, the actions and
/// the aside are slots a site fills with its words, the layout is fixed here, and it marks itself as the page's top
/// in the section index.
/// </summary>
public class SiteHeroTests
{
    /// <summary>Every slot lands in its place, in order, and the hero puts "Top" on the page's list of areas.</summary>
    [Test]
    public void TheHero_DrawsItsSlots_AndMarksTheTop()
    {
        using var ctx = new BunitContext();
        var areas = new PageAreas();

        RenderFragment hero = builder =>
        {
            builder.OpenComponent<SiteHero>(0);
            builder.AddComponentParameter(1, nameof(SiteHero.Topic), "A framework");
            builder.AddComponentParameter(2, nameof(SiteHero.Headline),
                (RenderFragment)(b => b.AddMarkupContent(0, "Watch every turn.")));
            builder.AddComponentParameter(3, nameof(SiteHero.Summary),
                (RenderFragment)(b => b.AddMarkupContent(0, "What it does.")));
            builder.AddComponentParameter(4, nameof(SiteHero.Note),
                (RenderFragment)(b => b.AddMarkupContent(0, "Who made it.")));
            builder.AddComponentParameter(5, nameof(SiteHero.Actions),
                (RenderFragment)(b => b.AddMarkupContent(0, "<a class=\"btn\" href=\"/go\">Go</a>")));
            builder.AddComponentParameter(6, nameof(SiteHero.Aside),
                (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"live\">Live</div>")));
            builder.CloseComponent();
        };
        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(hero));

        var section = cut.Find("section");
        var text = cut.Find(".hero-text").Children.Select(child => child.ClassName).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(section.Id, Is.EqualTo("top"));
            Assert.That(section.ClassList, Is.EquivalentTo(new[] { "hero", "with-aside" }));
            Assert.That(text, Is.EqualTo(new[] { "topic", "chrome", "summary", "muted", "actions" }));
            Assert.That(cut.Find("h1.chrome").TextContent, Is.EqualTo("Watch every turn."));
            Assert.That(cut.Find(".actions a.btn").TextContent, Is.EqualTo("Go"));
            Assert.That(cut.Find(".hero-aside .live").TextContent, Is.EqualTo("Live"));
            Assert.That(areas.Items, Is.EqualTo(new[] { new IndexEntry("top", "Top") }));
        });
    }

    /// <summary>
    /// With only a headline, the hero is one column of words: no topic, no summary, no actions, no aside.
    /// </summary>
    [Test]
    public void WithOnlyAHeadline_OneColumn_AndNoEmptySlots()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteHero>(parameters => parameters
            .Add(hero => hero.Headline, "Just this."));

        var section = cut.Find("section");
        Assert.Multiple(() =>
        {
            Assert.That(section.ClassList, Is.EquivalentTo(new[] { "hero" }));
            Assert.That(cut.Find(".hero-text").Children.Select(child => child.ClassName),
                Is.EqualTo(new[] { "chrome" }));
            Assert.That(cut.FindAll(".hero-aside"), Is.Empty);
        });
    }

    /// <summary>
    /// A hero may open with a piece of art above its words and stand centred (F151.6): the art is the first thing in
    /// the words' column, and the section says it is centred, so what stands beside the words goes below them.
    /// </summary>
    [Test]
    public void WithArt_AndCentred_TheArtOpensTheWords_AndTheSectionSaysSo()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteHero>(parameters => parameters
            .Add(hero => hero.Headline, "Ask the website.")
            .Add(hero => hero.Art, "<span class=\"drawing\">o</span>")
            .Add(hero => hero.Centred, true));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("section").ClassList, Is.EquivalentTo(new[] { "hero", "is-centred" }));
            Assert.That(cut.Find(".hero-text").Children.Select(child => child.ClassName),
                Is.EqualTo(new[] { "hero-art", "chrome" }));
            Assert.That(cut.Find(".hero-art .drawing").TextContent, Is.EqualTo("o"));
        });
    }
}

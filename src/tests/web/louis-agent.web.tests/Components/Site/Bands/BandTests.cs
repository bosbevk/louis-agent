using LouisAgent.Web.Components.Site.Bands;

namespace LouisAgent.Web.Tests.Components.Site.Bands;

/// <summary>
/// A band is one named area of a site's page (F117, ADR 0106): a section that must have an id, with a kicker over its
/// content, so every area on every site is named because the framework insists, and the guide can send a visitor to it.
/// </summary>
public class BandTests
{
    /// <summary>
    /// The id is on the section, the kicker opens its body, the content follows, and the classes a site adds come
    /// along.
    /// </summary>
    [Test]
    public void ABand_IsANamedSection_WithItsKicker()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, "levels")
            .Add(band => band.Kicker, "The three that exist")
            .Add(band => band.Class, "stands-on reveal")
            .AddChildContent("<h2>Level 0, 1 and 2</h2>"));

        var section = cut.Find("section");
        var body = cut.Find("section > .band-body");
        Assert.Multiple(() =>
        {
            Assert.That(section.Id, Is.EqualTo("levels"));
            Assert.That(section.ClassList, Is.EquivalentTo(new[] { "band", "stands-on", "reveal" }));
            Assert.That(body.FirstElementChild!.ClassName, Is.EqualTo("kicker"));
            Assert.That(body.FirstElementChild!.TextContent, Is.EqualTo("The three that exist"));
            Assert.That(body.QuerySelector("h2")!.TextContent, Is.EqualTo("Level 0, 1 and 2"));
        });
    }

    /// <summary>
    /// A band carries its own pager, hidden (F118, Louis's option 2): on a page of slides, a band with more than a
    /// screen of content is paged sideways by the frame's module, which shows the pager and fills in "1 / 3"; the
    /// buttons are named for a screen reader.
    /// </summary>
    [Test]
    public void ABand_CarriesAHiddenPager()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, "parts").AddChildContent("<p>Long.</p>"));

        var pager = cut.Find("section > .band-pager");
        var steps = pager.QuerySelectorAll("button.band-page");
        Assert.Multiple(() =>
        {
            Assert.That(pager.HasAttribute("hidden"), Is.True);
            Assert.That(steps.Select(b => b.GetAttribute("data-step")), Is.EqualTo(new[] { "-1", "1" }));
            Assert.That(steps.Select(b => b.GetAttribute("aria-label")),
                Is.EqualTo(new[] { "Previous page", "Next page" }));
            Assert.That(pager.QuerySelector(".band-pages"), Is.Not.Null);
        });
    }

    /// <summary>
    /// Without a class of its own, the section is a plain band; without a kicker, a strip, it has none.
    /// </summary>
    [Test]
    public void ABand_WithoutAClassOrAKicker_IsAPlainNamedSection()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, "contact").AddChildContent("<h2>Get in touch</h2>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("section").GetAttribute("class"), Is.EqualTo("band"));
            Assert.That(cut.FindAll(".kicker"), Is.Empty);
            Assert.That(cut.Find("section").Id, Is.EqualTo("contact"));
        });
    }

    /// <summary>
    /// A band that must stay whole is never paged (F122, Louis on the triage demo: "this must be one section"): it
    /// says so on the section, and the frame's module leaves it unpaged however tall it is; by default a band pages.
    /// </summary>
    [Test]
    public void ABandThatMustStayWhole_SaysSo()
    {
        using var ctx = new BunitContext();

        var whole = ctx.Render<Band>(parameters => parameters
            .Add(band => band.Id, "top").Add(band => band.Paged, false));
        var paged = ctx.Render<Band>(parameters => parameters.Add(band => band.Id, "top"));

        Assert.Multiple(() =>
        {
            Assert.That(whole.Find("section").HasAttribute("data-whole"), Is.True);
            Assert.That(paged.Find("section").HasAttribute("data-whole"), Is.False);
        });
    }
}

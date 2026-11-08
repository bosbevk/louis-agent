using LouisAgent.Web.Components.Site.Bands;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// A band is one named area of a site's page (F117, ADR 0106): a section that must have an id, with a kicker over its
/// content, so every area on every site is named because the framework insists, and the guide can send a visitor to it.
/// </summary>
public class BandTests
{
    /// <summary>The id is on the section, the kicker is its first child, the content follows, and the classes a site adds come along.</summary>
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
        Assert.Multiple(() =>
        {
            Assert.That(section.Id, Is.EqualTo("levels"));
            Assert.That(section.ClassList, Is.EquivalentTo(new[] { "band", "stands-on", "reveal" }));
            Assert.That(section.FirstElementChild!.ClassName, Is.EqualTo("kicker"));
            Assert.That(section.FirstElementChild!.TextContent, Is.EqualTo("The three that exist"));
            Assert.That(cut.Find("h2").TextContent, Is.EqualTo("Level 0, 1 and 2"));
        });
    }

    /// <summary>Without a class of its own, the section is a plain band; without a kicker, a strip, it has none.</summary>
    [Test]
    public void ABand_WithoutAClassOrAKicker_IsAPlainNamedSection()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Band>(parameters => parameters.Add(band => band.Id, "contact").AddChildContent("<h2>Get in touch</h2>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("section").GetAttribute("class"), Is.EqualTo("band"));
            Assert.That(cut.FindAll(".kicker"), Is.Empty);
            Assert.That(cut.Find("section").Id, Is.EqualTo("contact"));
        });
    }
}

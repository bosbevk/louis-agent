using LouisAgent.Web.Components.Site.Facts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Facts;

/// <summary>
/// A strip of facts is the framework's: each figure over what it counts, and a cell at its foot to add one.
/// </summary>
public class FactStripTests
{
    /// <summary>A fact is its figure in bold over its words.</summary>
    [Test]
    public void AFact_IsItsFigureOverItsWords()
    {
        using var ctx = new BunitContext();

        var fact = ctx.Render<Fact>(parameters => parameters
            .Add(f => f.Value, "17")
            .Add(f => f.Label, "years shipping software"));

        Assert.Multiple(() =>
        {
            Assert.That(fact.Find("div.fact > b").TextContent, Is.EqualTo("17"));
            Assert.That(fact.Find("div.fact > span").TextContent, Is.EqualTo("years shipping software"));
        });
    }

    /// <summary>The strip holds its facts, and its foot cell only when a site gives one.</summary>
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public void TheStrip_HasAFootCell_OnlyWhenGiven(bool add, int cells)
    {
        using var ctx = new BunitContext();
        RenderFragment button = builder => builder.AddMarkupContent(0, "<button>Add a fact</button>");

        var strip = ctx.Render<FactStrip>(parameters => parameters
            .AddChildContent("<div class=\"fact\">one</div>")
            .Add(s => s.Add, add ? button : null));

        Assert.Multiple(() =>
        {
            Assert.That(strip.FindAll("div.facts > .fact"), Has.Count.EqualTo(1));
            Assert.That(strip.FindAll("div.facts > .facts-add"), Has.Count.EqualTo(cells));
        });
    }
}

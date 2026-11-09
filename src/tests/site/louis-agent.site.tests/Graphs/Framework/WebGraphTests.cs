using LouisAgent.Site.Graphs.Framework;
using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Graphs.Framework;

/// <summary>
/// The web's diagram on How it works (F119) draws parts that exist: the site pack's tool by its name, the accounts the
/// API checks, the framework's web, and every arrow between boxes it has; and the page shows it on the web's slide.
/// </summary>
public class WebGraphTests
{
    /// <summary>
    /// The tool the guide moves the page with is named as the pack names it (the model's golden file pins that name),
    /// and the accounts and the web are there.
    /// </summary>
    [Test]
    public void TheBoxes_NameTheRealParts()
    {
        var labels = WebGraph.Spec.Nodes.Select(node => node.Label).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Does.Contain("ShowPage"));
            Assert.That(labels, Does.Contain("Accounts"));
            Assert.That(WebGraph.Spec.Nodes.Select(node => node.Note), Does.Contain("the framework's web"));
        });
    }

    /// <summary>Every arrow joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        var ids = WebGraph.Spec.Nodes.Select(node => node.Id).ToHashSet();

        Assert.That(WebGraph.Spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)), Is.Empty);
    }

    /// <summary>The page shows it on the web's slide.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HowItWorksPage>();

        Assert.That(cut.FindAll("section#web figure.graph g.node"), Has.Count.EqualTo(WebGraph.Spec.Nodes.Count));
    }
}

using LouisAgent.Site.Graphs;
using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Graphs;

/// <summary>
/// The Built-on-it diagram shows every demo in the repository (F111): a box per folder under <c>demos/</c>, by its
/// name, on the framework; and the page shows it once.
/// </summary>
public class DemosGraphTests
{
    /// <summary>
    /// Every folder under demos/ is a box (its id the folder's path, so a name that is also a wire name stays an id),
    /// and nothing else stands in that column.
    /// </summary>
    [Test]
    public void EveryDemo_IsABox()
    {
        var demos = Directory.GetDirectories(Path.Combine(TestPaths.RepoRoot, "demos"))
            .Select(Path.GetFileName).OrderBy(name => name).ToList();

        var boxes = DemosGraph.Spec.Nodes.Where(node => node.Column == 1).Select(node => node.Id.Split('/')[1])
            .OrderBy(id => id).ToList();

        Assert.That(boxes, Is.EqualTo(demos));
    }

    /// <summary>Every demo stands on the framework.</summary>
    [Test]
    public void EveryDemo_StandsOnTheFramework()
    {
        var onIt = DemosGraph.Spec.Edges.Where(edge => edge.From == "framework").Select(edge => edge.To).ToList();

        Assert.That(onIt,
            Is.EquivalentTo(DemosGraph.Spec.Nodes.Where(node => node.Column == 1).Select(node => node.Id)));
    }

    /// <summary>The page shows the diagram once.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<BuiltOnItPage>();

        Assert.That(cut.FindAll("figure.graph g.node"), Has.Count.EqualTo(DemosGraph.Spec.Nodes.Count));
    }
}

using LouisAgent.Site.Graphs.Framework;
using LouisAgent.Site.Pages.Framework;

namespace LouisAgent.Site.Tests.Graphs.Framework;

/// <summary>
/// The Architecture diagram's boxes are the repository's groups as the solution file has them (F111): every folder a
/// shipping project sits under is a box, the engine first; every arrow joins boxes that exist; the page shows it once.
/// </summary>
public class ArchitectureGraphTests
{
    /// <summary>
    /// The groups a shipping project's path starts with: <c>src/engine</c>, <c>src/hosts</c>… and <c>packs</c>,
    /// <c>demos</c>.
    /// </summary>
    private static List<string> Groups()
    {
        string[] onlyForTests = [".tests", ".integration-tests", ".testing", ".standin"];

        // A project line reads: Project("{type}") = "name", "path\name.csproj", "{id}". The path is the fourth quoted
        // part.
        return File.ReadLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"))
            .Where(line => line.StartsWith("Project(") && line.Contains(".csproj"))
            .Select(line => line.Split('"'))
            .Where(parts => !onlyForTests.Any(parts[3].EndsWith))
            .Select(parts => parts[5].Replace('\\', '/'))
            .Select(path => path.StartsWith("src/") ? string.Join('/', path.Split('/').Take(2)) : path.Split('/')[0])
            .Distinct().OrderBy(group => group).ToList();
    }

    /// <summary>Every group of the solution is a box, and no box is a group the solution doesn't have.</summary>
    [Test]
    public void TheBoxes_AreTheSolutionsGroups()
    {
        var boxes = ArchitectureGraph.Spec.Nodes.Select(node => node.Id).OrderBy(id => id).ToList();

        Assert.That(boxes, Is.EqualTo(Groups()));
    }

    /// <summary>The engine stands first, alone in its column, and everything else is to its right.</summary>
    [Test]
    public void TheEngine_StandsFirst()
    {
        var first = ArchitectureGraph.Spec.Nodes.Where(node => node.Column == 0).Select(node => node.Id).ToList();

        Assert.That(first, Is.EqualTo(new[] { "src/engine" }));
    }

    /// <summary>Every arrow joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        var ids = ArchitectureGraph.Spec.Nodes.Select(node => node.Id).ToHashSet();

        Assert.That(ArchitectureGraph.Spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)),
            Is.Empty);
    }

    /// <summary>The page shows the diagram once.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ArchitecturePage>();

        Assert.That(cut.FindAll("figure.graph g.node"), Has.Count.EqualTo(ArchitectureGraph.Spec.Nodes.Count));
    }
}

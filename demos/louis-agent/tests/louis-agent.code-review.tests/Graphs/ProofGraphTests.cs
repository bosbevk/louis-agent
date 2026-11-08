using LouisAgent.CodeReview.Graphs;
using LouisAgent.CodeReview.Graphs.Framework;
using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.CodeReview.Pages.Site.Framework;

namespace LouisAgent.CodeReview.Tests.Graphs;

/// <summary>
/// The Proof and Build-your-own diagrams say what their pages say (F111): the proof's chain ends in the ledger and a
/// re-score, the program's starts with your program and ends in the engine; each page shows its diagram once.
/// </summary>
public class ProofGraphTests
{
    /// <summary>The proof's chain: an eval, a run, its records, then a re-score and the ledger.</summary>
    [Test]
    public void TheChain_EndsInTheLedgerAndARescore()
    {
        var labels = ProofGraph.Spec.Nodes.OrderBy(node => node.Column).Select(node => node.Label).ToList();

        Assert.That(labels, Is.EqualTo(new[] { "An eval", "A run", "Its records", "A re-score", "The ledger" }));
    }

    /// <summary>The program's chain: your program, what it names and hands, a host, the engine.</summary>
    [Test]
    public void TheProgramsChain_StartsWithYoursAndEndsInTheEngine()
    {
        var labels = ProgramGraph.Spec.Nodes.OrderBy(node => node.Column).Select(node => node.Label).ToList();

        Assert.That(labels.First(), Is.EqualTo("Your program"));
        Assert.That(labels.Last(), Is.EqualTo("The engine"));
    }

    /// <summary>Every arrow of both joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        foreach (var spec in new[] { ProofGraph.Spec, ProgramGraph.Spec })
        {
            var ids = spec.Nodes.Select(node => node.Id).ToHashSet();
            Assert.That(spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)), Is.Empty);
        }
    }

    /// <summary>Each page shows its diagram once.</summary>
    [Test]
    public void ThePages_ShowThem()
    {
        using var ctx = new BunitContext();

        Assert.Multiple(() =>
        {
            Assert.That(ctx.Render<ProofPage>().FindAll("figure.graph g.node"), Has.Count.EqualTo(ProofGraph.Spec.Nodes.Count));
            Assert.That(ctx.Render<BuildYourOwnPage>().FindAll("figure.graph g.node"), Has.Count.EqualTo(ProgramGraph.Spec.Nodes.Count));
        });
    }
}

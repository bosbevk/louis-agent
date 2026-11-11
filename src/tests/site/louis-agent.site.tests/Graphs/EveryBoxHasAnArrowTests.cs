using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Site.Graphs;
using LouisAgent.Site.Graphs.Framework;

namespace LouisAgent.Site.Tests.Graphs;

/// <summary>
/// No box in any of the site's diagrams stands alone (FX85, Louis: "no lines, what are they?"): every box is joined
/// by at least one arrow, so a reader sees how it meets the rest. The pipeline's boxes had none.
/// </summary>
public class EveryBoxHasAnArrowTests
{
    /// <summary>Every diagram the site declares, by name.</summary>
    private static IEnumerable<TestCaseData> Diagrams()
    {
        yield return new TestCaseData(ArchitectureGraph.Spec).SetName("Architecture");
        yield return new TestCaseData(IdentityGraph.Spec).SetName("Identity");
        yield return new TestCaseData(PacksGraph.Spec).SetName("Packs");
        yield return new TestCaseData(ProgramGraph.Spec).SetName("Program");
        yield return new TestCaseData(TurnGraph.Spec).SetName("Turn");
        yield return new TestCaseData(DemosGraph.Spec).SetName("Demos");
        yield return new TestCaseData(ProofGraph.Spec).SetName("Proof");
    }

    /// <summary>Each box's id is the start or the end of some arrow.</summary>
    [TestCaseSource(nameof(Diagrams))]
    public void EveryBox_IsJoinedByAnArrow(GraphSpec spec)
    {
        var joined = spec.Edges.SelectMany(edge => new[] { edge.From, edge.To }).ToHashSet();

        var alone = spec.Nodes.Where(node => !joined.Contains(node.Id)).Select(node => node.Label).ToList();
        Assert.That(alone, Is.Empty, "boxes no arrow joins");
    }
}

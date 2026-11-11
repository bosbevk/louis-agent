using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// A flow box may list members under its label (CL60.4), as a class chart's type does: the box as wide as its longest
/// member and taller by each, the members in the code font under a divider.
/// </summary>
public class FlowMembersTests
{
    /// <summary>A long member of a speech provider's interface.</summary>
    private const string LongMember = "ISpeechToTextClient Create(SpeechOptions options, CancellationToken ct)";

    /// <summary>A box with the given members, alone in its flow, placed.</summary>
    private static double[] Size(params string[] members)
    {
        var node = new GraphNode("p", "ISpeechProvider", Members: members.Length == 0 ? null : members);
        var placed = GraphLayout.Place(new GraphSpec([node], [])).Nodes.Single();
        return [placed.W, placed.H];
    }

    /// <summary>A box is as wide as its longest member needs.</summary>
    [Test]
    public void ABoxWithMembers_IsWideEnoughForTheLongest() =>
        Assert.That(Size(LongMember)[0], Is.GreaterThanOrEqualTo(LongMember.Length * FlowText.WordChar));

    /// <summary>Each member makes the box taller by the same step, and a box with none keeps its height.</summary>
    [Test]
    public void EachMember_MakesTheBoxTaller()
    {
        double none = Size()[1], one = Size("Name")[1], two = Size("Name", "Create()")[1];

        Assert.Multiple(() =>
        {
            Assert.That(one, Is.GreaterThan(none));
            Assert.That(two - one, Is.EqualTo(FlowText.MemberStep).Within(0.001));
        });
    }

    /// <summary>The renderer draws each member, under a divider across the box.</summary>
    [Test]
    public void EveryMember_IsDrawnUnderADivider()
    {
        var spec = new GraphSpec([new GraphNode("p", "ISpeechProvider", Members: ["SpeechProviderName Name", LongMember])],
            []);

        string svg = DiagramRenderers.Render(new DiagramSource(DiagramKind.Flow, "Members", Flow: spec));

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain(">SpeechProviderName Name<"));
            Assert.That(svg, Does.Contain(">" + LongMember + "<"));
            Assert.That(svg, Does.Contain("class=\"frame\""));
        });
    }
}

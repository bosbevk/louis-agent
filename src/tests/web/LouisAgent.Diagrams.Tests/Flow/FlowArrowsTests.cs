using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Arrows;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tests.Flow.Arrows;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// The arrows in the joined look (CL60.2, Louis's pick "Both looks"): each in the tone of the box it leaves, its
/// words in a small chip, and its corners wide enough that a jog reads as a soft S, in the docs only.
/// </summary>
public class FlowArrowsTests
{
    /// <summary>A blue browser calling two boxes below it, one beside the other, so both arrows jog.</summary>
    private static GraphSpec Calls() => new(
        [
            new GraphNode("browser", "Browser app", Icon: DiagramIcon.Browser, Tone: DiagramTone.Blue),
            new GraphNode("identity", "Identity's routes"),
            new GraphNode("handlers", "AccountHandlers"),
            new GraphNode("tenant", "UseTenant")
        ],
        [
            new GraphEdge("browser", "identity", "POST /account/login"),
            new GraphEdge("browser", "handlers"),
            new GraphEdge("tenant", "handlers")
        ],
        FlowDirection.Down);

    /// <summary>
    /// Asked for wider corners, a jog's corner starts further from the turn: the site's eight pixels by default,
    /// the docs' soft bend when the renderer asks.
    /// </summary>
    [Test]
    public void AWiderBend_StartsItsCornerFurtherOut()
    {
        double Corner(double bend) => GraphLayout.Place(Calls(), bend: bend).Edges
            .SelectMany(edge => Ways.TurnStarts(edge.Way)).Max();

        Assert.Multiple(() =>
        {
            Assert.That(Corner(8), Is.EqualTo(8).Within(0.5));
            Assert.That(Corner(GraphLayout.SoftBend), Is.GreaterThan(8));
        });
    }

    /// <summary>
    /// A chip stands where its line is told from the others: on its own line when the chart runs right, the line
    /// through the chip's middle, and under the track it hangs from when the chart runs down (Louis's runner and
    /// usage-probe charts, CL60.2).
    /// </summary>
    [Test]
    public void AChip_StandsOnItsLine_OrUnderItsTrack()
    {
        var right = GraphLayout.Place(new GraphSpec([new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b", "--check")]), bend: GraphLayout.SoftBend).Edges.Single();
        var down = GraphLayout.Place(new GraphSpec(
            [
                new GraphNode("program", "Program.cs"), new GraphNode("gaps", "CacheGaps"),
                new GraphNode("env", "EnvFile")
            ],
            [new GraphEdge("program", "gaps", "--cache-gaps"), new GraphEdge("program", "env", "--host-culture")],
            FlowDirection.Down), bend: GraphLayout.SoftBend).Edges;

        var ground = FlowText.WordBox(right, Geometries.InUse);
        Assert.Multiple(() =>
        {
            Assert.That(ground.Middle.Y, Is.EqualTo(Ways.Ys(right.Way)[0]), "on its line");
            // The arrow to the box right under its source runs straight down and has no track: the other bends.
            var bent = down.Where(edge => Ways.Turns(edge.Way) > 0).ToList();
            Assert.That(bent, Has.Count.EqualTo(1));
            foreach (var edge in bent)
                Assert.That(FlowText.WordBox(edge, Geometries.InUse).Top,
                    Is.GreaterThanOrEqualTo(Track(edge.Way) + 4), "under its track");
        });
    }

    /// <summary>The height a down arrow runs across at: the one height its way holds for a while.</summary>
    private static double Track(ArrowPath way) => Ways.Ys(way).GroupBy(y => y).Where(same => same.Count() > 1)
        .Select(same => same.Key).Single();
}

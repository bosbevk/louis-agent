using System.Globalization;
using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
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
            .SelectMany(edge => Corners(edge.Path)).Max();

        Assert.Multiple(() =>
        {
            Assert.That(Corner(8), Is.EqualTo(8).Within(0.5));
            Assert.That(Corner(GraphLayout.SoftBend), Is.GreaterThan(8));
        });
    }

    /// <summary>An arrow wears the tone of the box it leaves, its head too; one from a plain box stays plain.</summary>
    [Test]
    public void AnArrow_WearsItsSourcesTone()
    {
        string svg = Render();

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(svg, "class=\"edge tone-blue\"[^>]*marker-end=\"url\\(#arrow-blue\\)\""),
                Has.Count.EqualTo(2));
            Assert.That(Regex.Matches(svg, "class=\"edge\"[^>]*marker-end=\"url\\(#arrow\\)\""), Has.Count.EqualTo(1));
            Assert.That(svg, Does.Contain("id=\"arrow-blue\""));
        });
    }

    /// <summary>A word stands in a chip of its arrow's tone, written as it is, not in capitals.</summary>
    [Test]
    public void AWord_StandsInAChip()
    {
        string svg = Render();

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("class=\"word-chip tone-blue\""));
            Assert.That(svg, Does.Contain("class=\"word tone-blue\""));
            Assert.That(svg, Does.Contain(">POST /account/login<"));
            Assert.That(Regex.Match(svg, @"\.word \{[^}]*\}").Value, Does.Not.Contain("uppercase"));
        });
    }

    /// <summary>
    /// A chip keeps clear of the line it belongs to: above its flat piece when the chart runs right, under the track
    /// it hangs from when the chart runs down (Louis's runner and usage-probe charts, CL60.2).
    /// </summary>
    [Test]
    public void AChip_KeepsClearOfItsLine()
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

        var (_, top, _, height) = FlowText.WordBox(right);
        Assert.Multiple(() =>
        {
            Assert.That(top + height, Is.LessThanOrEqualTo(Ys(right.Path).First() - 4), "above its line");
            foreach (var edge in down)
                Assert.That(FlowText.WordBox(edge).Y, Is.GreaterThanOrEqualTo(Track(edge.Path) + 4), "under its track");
        });
    }

    /// <summary>Every y a path passes: the second number of each pair after its commands.</summary>
    private static List<double> Ys(string path) => path.Split(' ')
        .Where(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).Where((_, at) => at % 2 == 1).ToList();

    /// <summary>The height a down arrow runs across at: the one y its path holds for a while.</summary>
    private static double Track(string path) => Ys(path).GroupBy(y => y).Where(same => same.Count() > 1)
        .Select(same => same.Key).Single();

    /// <summary>The flow drawn as the docs draw it.</summary>
    private static string Render() =>
        DiagramRenderers.Render(new DiagramSource(DiagramKind.Flow, "Calls", Flow: Calls()));

    /// <summary>
    /// How far each corner of a path starts from its turn: from the line's end to the curve's corner.
    /// </summary>
    private static IEnumerable<double> Corners(string path)
    {
        var parts = path.Split(' ');
        for (int at = 0; at + 4 < parts.Length; at++)
        {
            if (parts[at] != "Q") continue;

            double Number(int offset) => double.Parse(parts[at + offset], CultureInfo.InvariantCulture);
            // The line ends at the two numbers before the Q, and the Q's first pair is the corner itself.
            yield return Math.Abs(Number(1) - Number(-2)) + Math.Abs(Number(2) - Number(-1));
        }
    }
}

using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Cards.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;

/// <summary>
/// Draws the trunk of a cards picture that runs down (F169.5): one lit line down the picture's left with its glow,
/// and to each card it feeds a branch, an arrowhead where the link points at the card, and a node where the branch
/// leaves the trunk.
/// </summary>
/// <remarks>
/// The trunk wears the rail's own classes, <c>crd-rail</c> and <c>crd-rail-glow</c>, so every theme's rules for a
/// rail reach it with nothing added, flat's among them, and a branch is a rail's line. It is drawn as
/// <see cref="OverviewRails"/> draws a rail, turned on its side.
/// </remarks>
internal static class OverviewTrunks
{
    /// <summary>How thick the trunk's line and its glow are, and how big a node is: a rail's numbers.</summary>
    private const double Line = 1.5, Glow = 8, Node = 4.5;

    /// <summary>The tone of an arrowhead on a branch: the accent's first colour, as on a rail.</summary>
    private static readonly string Accent = DiagramTone.Violet.Wire();

    /// <summary>A trunk and its branches.</summary>
    public static void Draw(SvgWriter writer, PlacedTrunk trunk, IGeometry geometry)
    {
        // The glow and the line are two strips down the trunk's place, one over the other.
        double x = trunk.Top.X, top = trunk.Top.Y, length = trunk.Bottom.Y - trunk.Top.Y;
        writer.Box(geometry.Box(x - Glow / 2, top, Glow, length), 0, "crd-rail-glow")
            .Box(geometry.Box(x - Line / 2, top, Line, length), 0, "crd-rail");

        // A branch from the trunk across to its card's edge.
        foreach (var end in trunk.Branches)
        {
            string path = new PathWriter().From(x, end.At.Y).Across(end.At.X).Text;
            writer.Path(path, "crd-line", arrow: end.Arrow, tone: end.Arrow ? Accent : null);
        }

        // The nodes last, over the branches' starts.
        foreach (var end in trunk.Branches) writer.Circle(x, end.At.Y, Node, "mrq-node");
    }
}

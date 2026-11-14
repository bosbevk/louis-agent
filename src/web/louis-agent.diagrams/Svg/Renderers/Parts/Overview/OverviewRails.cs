using LouisAgent.Choices;
using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Overview;

/// <summary>
/// Draws a rail of a cards picture: the lit line with its glow, fading past its outer ends, and for each end a line
/// to its card, a node where that line meets the rail, an arrowhead where a link points at the card, and dots that
/// run down it.
/// </summary>
internal static class OverviewRails
{
    /// <summary>
    /// How far past its outer ends a rail runs as it fades, how far it may run past the picture's sides, how thick
    /// its line and its glow are, and how big a node is.
    /// </summary>
    private const double Reach = 84, Over = 8, Line = 1.5, Glow = 8, Node = 4.5;

    /// <summary>The tone of an arrowhead on a rail's line: the accent's first colour.</summary>
    private static readonly string Accent = DiagramTone.Violet.Wire();

    /// <summary>A rail and its ends, in a picture that wide.</summary>
    public static void Draw(SvgWriter writer, PlacedRail rail, double width)
    {
        double left = Math.Max(-Over, rail.Left - Reach), right = Math.Min(width + Over, rail.Right + Reach);
        writer.Box(left, rail.Y - Glow / 2, right - left, Glow, 0, "crd-rail-glow")
            .Box(left, rail.Y - Line / 2, right - left, Line, 0, "crd-rail");

        // Dots say one way, so they run only where every link of the rail points down.
        bool down = rail.Ends.All(end => end.Arrow != end.Above);
        foreach (var end in rail.Ends)
        {
            // From the top down, whichever side of the rail the card is on, so the dots fall and never rise.
            string path = $"M{SvgWriter.N(end.X)} {SvgWriter.N(Math.Min(end.Y, rail.Y))}" +
                          $"V{SvgWriter.N(Math.Max(end.Y, rail.Y))}";
            writer.Path(path, "crd-line", arrow: end is { Arrow: true, Above: false },
                arrowAtStart: end is { Arrow: true, Above: true }, tone: end.Arrow ? Accent : null);
            if (down) writer.Path(path, "pic-march", arrow: false);
        }

        // The nodes last, over the lines' ends.
        foreach (var end in rail.Ends) writer.Circle(end.X, rail.Y, Node, "mrq-node");
    }
}

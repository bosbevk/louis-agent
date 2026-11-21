using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Arrows;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// What a reader would see wrong in a placed flow (CL60.2, Louis: "you must look at each one"): a word outside the
/// drawing or on a box, and a line through a box it doesn't join. It asks each arrow's way for its stretches and
/// each box for its edges.
/// </summary>
internal static class Clearance
{
    /// <summary>
    /// A pixel: how far inside a box a line must pass to cross it, and how near two numbers are the same place.
    /// And how much of a line two stretches must share to lie on one another, more than a rounded corner's width.
    /// </summary>
    private const double Pixel = 1, CornerWide = 2;

    /// <summary>The geometry a word's ground is made with: the one in use.</summary>
    private static readonly IGeometry Geometry = Geometries.InUse;

    /// <summary>Every fault in the drawing, said in words.</summary>
    public static IEnumerable<string> Faults(PlacedGraph placed)
    {
        foreach (var edge in placed.Edges)
        {
            var own = edge.Way.Stretches.ToList();
            foreach (var box in placed.Nodes.Where(box => box.Node.Id != edge.Edge.From && box.Node.Id != edge.Edge.To))
                if (own.Any(stretch => Crosses(stretch, box.Box)))
                    yield return $"the line {edge.Edge.From}>{edge.Edge.To} crosses {box.Node.Id}";

            if (edge.Edge.Word is not { } word) continue;
            var ground = FlowText.WordBox(edge, Geometry);
            if (ground.Left < 0 || ground.Top < 0 || ground.Right > placed.Width || ground.Bottom > placed.Height)
                yield return $"the word '{word}' leaves the drawing";
            foreach (var box in placed.Nodes.Where(box => Overlap(ground, box.Box)))
                yield return $"the word '{word}' sits on {box.Node.Id}";
            // A word may stand on its own arrow's line (F134.8). Another arrow of the same box shares that line
            // where the box's one line out has not branched yet, so a piece of it that lies on this arrow's own
            // is not another line.
            bool Shared(PlacedEdge other, ArrowStretch piece) =>
                other.Edge.From == edge.Edge.From && own.Any(mine => OnOneAnother(mine, piece));
            foreach (var other in placed.Edges.Where(other => other != edge
                                                              && other.Way.Stretches.Any(piece =>
                                                                  Covers(piece, ground) && !Shared(other, piece))))
                yield return $"the word '{word}' lies on the line {other.Edge.From}>{other.Edge.To}";
            // Each pair of words once: this word against the words of the arrows after it.
            foreach (var other in placed.Edges.SkipWhile(other => other != edge).Skip(1)
                         .Where(other => other.Edge.Word is not null
                                         && Overlap(ground, FlowText.WordBox(other, Geometry))))
                yield return $"the word '{word}' overlaps the word '{other.Edge.Word}'";
        }
    }

    /// <summary>
    /// Every pair of forward lines from different boxes that lie on one another for a stretch (F134.8): what arrows
    /// that join do by design, and what arrows drawn apart may never do. Two arrows of one box share its line out,
    /// so they are not a pair.
    /// </summary>
    public static IEnumerable<string> Lying(PlacedGraph placed)
    {
        var column = placed.Nodes.ToDictionary(node => node.Node.Id, node => node.Node.Column);
        var forward = placed.Edges.Where(edge => column.GetValueOrDefault(edge.Edge.To)
                                                 > column.GetValueOrDefault(edge.Edge.From)).ToList();

        // Each pair once: a line against the lines after it.
        foreach (var (line, at) in forward.Select((line, at) => (line, at)))
            foreach (var other in forward.Skip(at + 1).Where(other => other.Edge.From != line.Edge.From))
                if (line.Way.Stretches.Any(piece => other.Way.Stretches.Any(second => OnOneAnother(piece, second))))
                    yield return $"the line {line.Edge.From}>{line.Edge.To} lies on {other.Edge.From}>{other.Edge.To}";
    }

    /// <summary>
    /// Whether two stretches run along the same line and share more of it than a rounded corner's width.
    /// </summary>
    private static bool OnOneAnother(ArrowStretch piece, ArrowStretch second)
    {
        bool flat = Same(piece.From.Y, piece.To.Y) && Same(second.From.Y, second.To.Y)
                                                   && Same(piece.From.Y, second.From.Y);
        bool upright = Same(piece.From.X, piece.To.X) && Same(second.From.X, second.To.X)
                                                      && Same(piece.From.X, second.From.X);
        return flat && Math.Min(piece.Right, second.Right) - Math.Max(piece.Left, second.Left) > CornerWide
               || upright && Math.Min(piece.Bottom, second.Bottom) - Math.Max(piece.Top, second.Top) > CornerWide;
    }

    /// <summary>Whether two numbers are the same place on the drawing, to less than a pixel.</summary>
    private static bool Same(double a, double b) => Math.Abs(a - b) < Pixel;

    /// <summary>Whether two boxes overlap, as two words' ground may not, nor a word's and a box.</summary>
    private static bool Overlap(IBox a, IBox b) =>
        a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

    /// <summary>Whether a stretch passes through the ground a word covers.</summary>
    private static bool Covers(ArrowStretch piece, IBox ground) =>
        piece.Left < ground.Right && piece.Right > ground.Left && piece.Top < ground.Bottom
        && piece.Bottom > ground.Top;

    /// <summary>Whether a stretch passes through a box's inside, its edges a pixel in.</summary>
    private static bool Crosses(ArrowStretch stretch, IBox box) =>
        stretch.Left < box.Right - Pixel && stretch.Right > box.Left + Pixel && stretch.Top < box.Bottom - Pixel
        && stretch.Bottom > box.Top + Pixel;
}

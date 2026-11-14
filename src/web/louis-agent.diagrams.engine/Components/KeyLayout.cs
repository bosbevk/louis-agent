using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Keys;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Tones;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Components;

/// <summary>
/// Places the key of a component diagram (F134.7): "how to read it", with a row for each thing the picture really
/// uses, so a small picture gets a small key.
/// </summary>
/// <remarks>
/// The key is part of the picture, as it was in the look Louis picked (of the first drawing without it: "where is
/// this?"). It stands under the column before the root, where the picture has room, and under the root itself
/// when nothing stands before it.
/// </remarks>
internal static class KeyLayout
{
    /// <summary>
    /// How wide the key is. The column before the root is never narrower, so the key stays clear of the root.
    /// </summary>
    public const double Width = 250;

    /// <summary>The room its heading takes, and one row's height.</summary>
    public const double Heading = 30, Row = 28;

    /// <summary>The room between the key and what stands over it.</summary>
    private const double Above = 40;

    /// <summary>The key at its place, with its rows.</summary>
    public static PlacedKey Of(ComponentsSpec components, IBox root, IReadOnlyList<PlacedNeighbour> neighbours,
        IGeometry geometry)
    {
        var rows = Rows(components).ToList();
        var before = neighbours.Where(neighbour => neighbour.Neighbour.Side == NeighbourSide.Before).ToList();

        // Under the column before the root. With no such column, under the root, from its left edge.
        double over = before.Count == 0 ? root.Bottom : before.Max(neighbour => neighbour.Box.Bottom);
        double x = before.Count == 0 ? root.Left : 0;
        return new PlacedKey(geometry.Box(x, over + Above, Width, Heading + rows.Count * Row), rows);
    }

    /// <summary>The row a key's thing number so-many stands in: under the heading, as wide as the key.</summary>
    public static IBox RowBox(PlacedKey key, int at, IGeometry geometry) =>
        geometry.Box(key.Box.Left, key.Box.Top + Heading + at * Row, key.Box.Width, Row);

    /// <summary>
    /// A row for each thing the picture uses: its tones in their order, a pin each way, a read, the one thing a
    /// box holds, and that a box inside a box is held by it.
    /// </summary>
    private static IEnumerable<KeyRow> Rows(ComponentsSpec components)
    {
        var boxes = components.Root.Within().ToList();
        var around = components.Around ?? [];
        var tones = boxes.Select(box => box.Tone).Concat(around.Select(neighbour => neighbour.Tone))
            .Where(tone => tone != DiagramTone.Plain).Distinct().Order();
        foreach (var tone in tones) yield return new KeyRow(KeyRowKind.Tone, ToneMeanings.Of(tone), tone);

        bool handsDown = boxes.Any(box => box.In?.Count > 0), saysBack = boxes.Any(box => box.Out?.Count > 0);
        bool reads = around.Any(neighbour => neighbour.Reads), holds = boxes.Any(box => box.State is not null);
        bool nests = boxes.Any(box => box.Holds?.Count > 0);

        // A sample wears the colour of the thing it shows: a pin the root's, a pill its holder's.
        var own = components.Root.Tone;
        var holder = boxes.FirstOrDefault(box => box.State is not null)?.Tone ?? own;
        if (handsDown) yield return RowOf(KeyRowKind.HandedDown, DrawingMessage.ComponentParameter, own);
        if (saysBack) yield return RowOf(KeyRowKind.SaidBackUp, DrawingMessage.ComponentCallback, own);
        if (reads) yield return RowOf(KeyRowKind.Reads, DrawingMessage.ComponentReadsAService);
        if (holds) yield return RowOf(KeyRowKind.Holds, DrawingMessage.ComponentHolds, holder);
        if (nests) yield return RowOf(KeyRowKind.Sentence, DrawingMessage.ComponentHeldBy);
    }

    /// <summary>A row with its words from the messages, in the tone its sample wears.</summary>
    private static KeyRow RowOf(KeyRowKind kind, DrawingMessage words, DiagramTone tone = DiagramTone.Plain) =>
        new(kind, DiagramText.Format(words), tone);
}

using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Sequence.Keys;

/// <summary>
/// Places a sequence's key under its drawing (F134.10): "how to read it", then an item for each tone a badge wears,
/// a call, an answer, a note and each kind of block, only where the picture really has one, side by side across
/// the drawing's width and on to a new row when a row is full.
/// </summary>
/// <remarks>
/// A pure function of the data, as a flow's key is of its boxes, so a key never explains something the picture
/// doesn't have. In this look the key matters more than in a flow: a call has no tone, so the badges are the only
/// place a colour says something, and the key is what tells the reader so.
/// </remarks>
public static class SequenceKeyLayout
{
    /// <summary>The room the key's heading takes, and how tall a row of items is.</summary>
    public const double Heading = 34, Row = 28;

    /// <summary>How far after its sample an item's words start.</summary>
    public const double Words = 10;

    /// <summary>
    /// How wide a character of an item's words is taken to be, generously, since no font is measured, and the room
    /// between two items.
    /// </summary>
    private const double Char = 6.6, Gap = 30;

    /// <summary>
    /// How wide a sample is: a tone's swatch, a call's or an answer's line, and a note's panel. A block's is its
    /// kind's pill, as wide as the picture draws it.
    /// </summary>
    private const double Swatch = 18, Line = 48, Panel = 40;

    /// <summary>The key's items for a sequence, across a width, and how tall the key is.</summary>
    public static (IReadOnlyList<SequenceKeyItem> Items, double Height) Of(SequenceSpec spec, double width)
    {
        var steps = spec.Steps.SelectMany(step => step.All()).ToList();

        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        var items = new List<SequenceKeyItem>();
        double x = 0, y = Heading;
        foreach (var item in Tones(spec).Concat(Lines(steps)).Concat(Blocks(steps)))
        {
            double room = item.Sample + Words + item.Words.Length * Char;
            if (x > 0 && x + room > width) (x, y) = (0, y + Row);
            items.Add(item with { X = x, Y = y });
            x += room + Gap;
        }

        return (items, y + Row);
    }

    /// <summary>An item for each tone a badge wears, in the tones' own order, with what the tone means.</summary>
    private static IEnumerable<SequenceKeyItem> Tones(SequenceSpec spec) => spec.Participants
        .Select(head => head.Tone).Where(tone => tone != DiagramTone.Plain).Distinct().Order()
        .Select(tone => new SequenceKeyItem(SequenceKeyKind.Tone, ToneMeanings.Of(tone), 0, 0, Swatch, tone));

    /// <summary>A call, an answer and a note, each only when the picture has one.</summary>
    private static IEnumerable<SequenceKeyItem> Lines(List<SequenceStep> steps)
    {
        if (steps.OfType<MessageStep>().Any())
            yield return new SequenceKeyItem(SequenceKeyKind.Call, Text(DrawingMessage.SequenceCall), 0, 0, Line);
        if (steps.OfType<ReplyStep>().Any())
            yield return new SequenceKeyItem(SequenceKeyKind.Answer, Text(DrawingMessage.SequenceAnswer), 0, 0, Line);
        if (steps.OfType<NoteStep>().Any())
            yield return new SequenceKeyItem(SequenceKeyKind.Note, Text(DrawingMessage.SequenceNote), 0, 0, Panel);
    }

    /// <summary>
    /// An item for each kind of block the picture has, in the kinds' own order, with what the kind does. Its
    /// sample is the kind's pill, as wide as a frame's own.
    /// </summary>
    private static IEnumerable<SequenceKeyItem> Blocks(List<SequenceStep> steps) => steps.OfType<BlockStep>()
        .Select(block => block.Block).Distinct().Order()
        .Select(kind => new SequenceKeyItem(SequenceKeyKind.Block, Text(Meaning(kind)), 0, 0,
            SequenceMeasures.Marquee.KindWidth(kind), Block: kind));

    /// <summary>The message that says what a kind of block does.</summary>
    private static DrawingMessage Meaning(BlockKind kind) => kind switch
    {
        BlockKind.Loop => DrawingMessage.SequenceLoop,
        BlockKind.Opt => DrawingMessage.SequenceOpt,
        _ => DrawingMessage.SequenceAlt
    };

    /// <summary>A message's words, which have no holes.</summary>
    private static string Text(DrawingMessage message) => DiagramText.Format(message);
}

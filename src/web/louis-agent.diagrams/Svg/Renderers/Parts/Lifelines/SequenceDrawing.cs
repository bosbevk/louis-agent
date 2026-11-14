using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Keys;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence in the cards' look (F134.10, Louis's pick "Marquee"): its own small title, a light behind the
/// row of participants, their lifelines, the blocks' frames, each call, answer and note, and the key under it all.
/// </summary>
/// <remarks>
/// It draws what <see cref="SequenceLayout"/> placed by <see cref="SequenceMeasures.Marquee"/> and decides no
/// place itself. The look is the shared <see cref="PictureLook"/>, then <see cref="MarqueeLook"/>, which a cards
/// picture wears too, then what <see cref="SequenceStyle"/> adds. The drawing names no colour: a part gets a
/// class, and a fade gets a paint the style fills with. <see cref="SequenceHeads"/> draws a participant and its
/// lifeline, <see cref="SequenceFrames"/> a block, <see cref="SequenceSteps"/> a call, an answer and a note, and
/// <see cref="SequenceKey"/> the key.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class SequenceDrawing
{
    /// <summary>
    /// The room the picture's title takes above the participants, where its words stand in it, the room between
    /// the lifelines' ends and the key, and the room a caption takes under the key.
    /// </summary>
    private const double Head = 48, TitleLine = 14, KeyGap = 12, CaptionRoom = 34;

    /// <summary>How wide a character of the title is taken to be, generously, since no font is measured.</summary>
    private const double TitleChar = 7;

    /// <summary>
    /// The light behind the participants: how far under the badges' top its middle is, and how far it reaches
    /// down. Across, it reaches the picture's sides, where it has faded to nothing.
    /// </summary>
    private const double LightDrop = 36, LightDown = 170;

    /// <summary>The sequence a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source)
    {
        var placed = SequenceLayout.Place(source.Sequence!, SequenceMeasures.Marquee);

        // As wide as the participants, or as its own title when that is wider. The key goes across that width.
        double width = Math.Max(placed.Width, source.Title.Length * TitleChar);
        var (items, keyHeight) = SequenceKeyLayout.Of(source.Sequence!, width);
        double keyTop = Head + placed.Height + KeyGap;
        double height = keyTop + keyHeight + (source.Caption is null ? 0 : CaptionRoom);

        // The light first, so everything stands on it. Then the steps, and the key under them.
        var writer = Light(Paints(new SvgWriter()), width)
            .Text(0, TitleLine, source.Title, "mrq-title", "start")
            .Group("seq-drawing", 0, Head, Steps(placed))
            .Group("seq-legend", 0, keyTop, SequenceKey.Of(items, width));
        if (source.Caption is { } caption) writer.Text(0, height - 8, caption, "caption", "start");

        // What every picture in the new look wears, then what a cards picture wears too, then a sequence's own.
        string style = PictureLook.Of(SvgTheme.Site) + MarqueeLook.Rules + SequenceStyle.Of(SvgTheme.Site);
        return writer.Write(SvgTheme.Site, source.Title, width, height, style);
    }

    /// <summary>
    /// The participants and their steps, from where the layout starts, back to front: the frames' panels, the
    /// outer under the inner, the lifelines over them, the frames' tabs over the lifelines, then each step, and
    /// the participants last.
    /// </summary>
    private static SvgWriter Steps(PlacedSequence placed)
    {
        var writer = new SvgWriter();
        var frames = placed.Blocks.OrderBy(block => block.Depth).ToList();
        foreach (var block in frames) SequenceFrames.Panel(writer, block);
        foreach (var head in placed.Heads) SequenceHeads.Lifeline(writer, head, placed.Height);
        foreach (var block in frames) SequenceFrames.Tabs(writer, block);
        foreach (var row in placed.Rows) SequenceSteps.Draw(writer, row);
        foreach (var head in placed.Heads) SequenceHeads.Draw(writer, head);
        return writer;
    }

    /// <summary>
    /// The look's six fades, each under the id the style fills with: the light from its middle out, a call's beam
    /// across and its mirror for a call to the left, a lifeline down, a frame's top edge across, and the glass
    /// down.
    /// </summary>
    private static SvgWriter Paints(SvgWriter writer) => writer
        .Paint(MarqueeLook.LightPaint, PaintRun.Out, 0, 0.5, 1)
        .Paint(SequenceStyle.BeamPaint, PaintRun.Across, 0, 1)
        .Paint(SequenceStyle.BackPaint, PaintRun.Across, 0, 1)
        .Paint(SequenceStyle.LifePaint, PaintRun.Down, 0, 0.85, 1)
        .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
        .Paint(MarqueeLook.GlassPaint, PaintRun.Down, 0, 1);

    /// <summary>The light behind the participants: an oval round the row of badges, as wide as the picture.</summary>
    private static SvgWriter Light(SvgWriter writer, double width)
    {
        double reach = width / 2 + SvgWriter.Pad, y = Head + LightDrop;
        string across = SvgWriter.N(reach), down = SvgWriter.N(LightDown), there = SvgWriter.N(2 * reach);
        return writer.Path(
            $"M{SvgWriter.N(-SvgWriter.Pad)} {SvgWriter.N(y)}a{across} {down} 0 1 0 {there} 0" +
            $"a{across} {down} 0 1 0 -{there} 0Z", "mrq-light", false);
    }
}

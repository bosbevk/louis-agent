using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Engine.Sequence.Keys;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

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
    /// A sequence's own numbers for its picture: more room above, for the participants' badges, less before the
    /// key, and a light a little lower and shallower, behind the participants.
    /// </summary>
    private static readonly PictureMeasures Measures = new(Head: 48, KeyGap: 12, LightDrop: 36, LightDown: 170);

    /// <summary>The sequence a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var spec = source.Sequence!;
        var placed = SequenceLayout.Place(spec, SequenceMeasures.Marquee, geometry);

        // The steps are the body, and the key goes across under them. Five fades are a sequence's to ask for: a
        // call's beam and its mirror for a call to the left, a lifeline, a frame's top edge, and the glass.
        return PictureBuilder.Titled(source.Title, Measures)
            .Type(TypeNames.Of(source))
            .Paint(SequenceStyle.BeamPaint, PaintRun.Across, 0, 1)
            .Paint(SequenceStyle.BackPaint, PaintRun.Across, 0, 1)
            .Paint(SequenceStyle.LifePaint, PaintRun.Down, 0, 0.85, 1)
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Paint(MarqueeLook.GlassPaint, PaintRun.Down, 0, 1)
            .Body("seq-drawing", Steps(placed, geometry), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = SequenceKeyLayout.Of(spec, width);
                return (height, (writer, top) =>
                    writer.Group("seq-legend", 0, top, SequenceKey.Of(items, width, geometry)));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, SequenceStyle.Of(SvgTheme.Site))
            .Build();
    }

    /// <summary>
    /// The participants and their steps, from where the layout starts, back to front: the frames' panels, the
    /// outer under the inner, the lifelines over them, the frames' tabs over the lifelines, then each step, and
    /// the participants last.
    /// </summary>
    private static SvgWriter Steps(PlacedSequence placed, IGeometry geometry)
    {
        var writer = new SvgWriter();
        var frames = placed.Blocks.OrderBy(block => block.Depth).ToList();
        foreach (var block in frames) SequenceFrames.Panel(writer, block, geometry);
        foreach (var head in placed.Heads) SequenceHeads.Lifeline(writer, head, placed.Height, geometry);
        foreach (var block in frames) SequenceFrames.Tabs(writer, block, geometry);
        foreach (var row in placed.Rows) SequenceSteps.Draw(writer, row, geometry);
        foreach (var head in placed.Heads) SequenceHeads.Draw(writer, head, geometry);
        return writer;
    }
}

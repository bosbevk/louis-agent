using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Keys;
using LouisAgent.Diagrams.Engine.Sequence.Listed;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.StepLists;

/// <summary>
/// Draws a sequence as steps down a phone's width (F169.7): its title with the type under it, the blocks' frames,
/// each call, answer and note in the order they happen, and the sequence's own key under it all.
/// </summary>
/// <remarks>
/// A second drawing for the data a sequence already is, beside <see cref="SequenceDrawing"/>: it draws what
/// <see cref="StepListLayout"/> placed and decides no place itself. The frames are the wide drawing's
/// (<see cref="SequenceFrames"/>), the key is the wide drawing's laid out for this width
/// (<see cref="SequenceKeyLayout"/>), and the styles are the sequence's with what
/// <see cref="SequenceStyle.StepList"/> adds, so a reader who knows one drawing reads the other.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class StepListDrawing
{
    /// <summary>
    /// The sequence's own numbers for its picture, as the wide drawing has them, with less room above, since no row
    /// of participants stands under the title.
    /// </summary>
    private static readonly PictureMeasures Measures = new(KeyGap: 12);

    /// <summary>The sequence a source holds, drawn as steps for a phone, as one SVG file's text.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var spec = source.Sequence!;
        var placed = StepListLayout.Place(spec, PageSvg.NarrowLayout, StepListMeasures.Marquee, geometry);

        // The steps are the body, and the key goes across under them. The fades are the wide drawing's: a call's
        // beam and its mirror, a frame's top edge, and the glass.
        return PictureBuilder.Titled(source.Title, geometry, Measures)
            .Type(TypeNames.Of(source))
            .Fit(PictureFit.Narrow)
            .Paint(SequenceStyle.BeamFade)
            .Paint(SequenceStyle.BackFade)
            .Paint(MarqueeLook.EdgeFade)
            .Paint(MarqueeLook.GlassFade)
            .Body("stl-drawing", Steps(placed, geometry), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = SequenceKeyLayout.Of(spec, width);
                return (height, (writer, top) =>
                    writer.Group("seq-legend", 0, top, SequenceKey.Of(items, width, geometry)));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, SequenceStyle.Of(SvgTheme.Site),
                SequenceStyle.StepList(SvgTheme.Site))
            .Build();
    }

    /// <summary>
    /// The frames and the steps, from where the layout starts, back to front: the frames' panels, the outer under
    /// the inner, then their tabs, then each step.
    /// </summary>
    private static SvgWriter Steps(PlacedStepList placed, IGeometry geometry)
    {
        var writer = new SvgWriter();
        var frames = placed.Blocks.OrderBy(block => block.Depth).ToList();
        foreach (var block in frames) SequenceFrames.Panel(writer, block, geometry);
        foreach (var block in frames) SequenceFrames.Tabs(writer, block, geometry);
        foreach (var step in placed.Steps) StepListSteps.Draw(writer, step, geometry);
        return writer;
    }
}

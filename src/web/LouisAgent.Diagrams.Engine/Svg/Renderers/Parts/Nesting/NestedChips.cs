using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Nested;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;

/// <summary>
/// Draws a chip (F134.7): a few words in a tone, in a small box as round as it is tall. A component's kind
/// beside its name is one, a neighbour's kind under its name, and a part's role in a composite structure.
/// </summary>
/// <remarks>
/// It was drawn in two places, each with the chip's height and where its words stand written out, so the two
/// looked alike only while the copies agreed. Now a chip is drawn here, once.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal static class NestedChips
{
    /// <summary>A chip's height, and where its words start in it: across, and down to their line.</summary>
    private const double Height = 18, WordsIn = 8, WordsLine = 12.5;

    /// <summary>A chip from its left and its top, as wide as its words need.</summary>
    public static SvgWriter Draw(SvgWriter writer, IGeometry geometry, double left, double top, string words,
        string tone)
    {
        var chip = geometry.Box(left, top, NestedSizes.ChipWidth(words), Height);
        return writer.Box(chip, chip.HalfHeight, $"pic-tint{tone}")
            .Text(chip.Left + WordsIn, chip.Top + WordsLine, words, $"cmp-chip-text pic-ink{tone}", "start");
    }
}

using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Theme;

/// <summary>
/// One look's colours: the ground, a hairline, the text, the quiet text and lines, a chip's fill, the accent, and
/// each tone a box may wear.
/// </summary>
public sealed record SvgPalette(
    string Ground,
    string Line,
    string Text,
    string Quiet,
    string Chip,
    string Accent,
    IReadOnlyDictionary<DiagramTone, SvgTone> Tones);

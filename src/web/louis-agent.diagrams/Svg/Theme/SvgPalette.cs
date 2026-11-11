using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// One look's colours: the ground, a box's fill and line, the text, the brightest text, the quiet text and lines, a
/// code chip's fill, the accent's line and its tint, which a highlighted card wears, the docs' title bar, and each
/// tone a box may wear.
/// </summary>
public sealed record SvgPalette(
    string Ground,
    string Panel,
    string Line,
    string Text,
    string Bright,
    string Quiet,
    string Chip,
    string Accent,
    string Tint,
    SvgTone Banner,
    IReadOnlyDictionary<DiagramTone, SvgTone> Tones);

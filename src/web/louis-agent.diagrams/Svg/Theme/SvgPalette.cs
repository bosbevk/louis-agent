namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// One look's colours: the ground, a box's fill and line, the text, the brightest text, the quiet text and lines, a
/// code chip's fill, and the accent's line and its tint, which a highlighted card wears.
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
    string Tint);

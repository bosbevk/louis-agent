namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// One tone's colours on one screen: the tint a box is filled with, the line its border, badge, arrows and word chips
/// are drawn in, and the ink its words are written in.
/// </summary>
public sealed record SvgTone(string Fill, string Line, string Ink);

using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Tones;

/// <summary>
/// The colour a box wears, by the names a diagram's file uses: plain, the quiet grey every box had, or one of six
/// hues. A box takes one tone, and its arrows and their words take its tone too.
/// </summary>
/// <remarks>
/// Hues by name, not by meaning (the "type code kept as an enum" of Fowler's catalogue, as <c>CardLook</c> is): a
/// diagram's author says which boxes belong together by giving them one hue, and <see cref="Svg.Theme.SvgTheme"/>
/// says what each hue looks like on a dark and a light screen, so one file serves the docs, the site and the chat.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramTone
{
    [WireName("plain")] Plain,
    [WireName("blue")] Blue,
    [WireName("green")] Green,
    [WireName("violet")] Violet,
    [WireName("teal")] Teal,
    [WireName("red")] Red,
    [WireName("amber")] Amber
}

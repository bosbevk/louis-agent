using System.Globalization;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// How far two colours stand apart, by the web's contrast sum, and the two numbers the web's guidelines ask for: 4.5
/// to 1 for words, 3 to 1 for a ring or a line you must see. A test asks it of a theme's values, so a look the eye
/// can't judge is worked out and pinned.
/// </summary>
/// <remarks>
/// The sum is the one W3C's technique G18 writes today, with its cut-off at 0.04045
/// (https://www.w3.org/WAI/WCAG22/Techniques/general/G18). It lives on one type so every test that asks about
/// contrast asks the same sum, rather than each working it out where it stands (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class Contrast
{
    /// <summary>The least contrast for ordinary words, as the web's guidelines ask (WCAG 1.4.3).</summary>
    public const double ForWords = 4.5;

    /// <summary>
    /// The least contrast for a ring or a line you must see, as the web's guidelines ask (WCAG 1.4.11).
    /// </summary>
    public const double ForARing = 3;

    /// <summary>
    /// How far two colours written as <c>#rrggbb</c> stand apart, from 1 (the same colour) to 21 (black on white). The
    /// order doesn't matter.
    /// </summary>
    public static double Between(string first, string second)
    {
        double lighter = Math.Max(Lightness(first), Lightness(second));
        double darker = Math.Min(Lightness(first), Lightness(second));
        return (lighter + Flare) / (darker + Flare);
    }

    /// <summary>The bit of light every screen adds, which keeps black from dividing by nothing.</summary>
    private const double Flare = 0.05;

    /// <summary>Below this a channel's light grows in a straight line, above it on the curve (G18's cut-off).</summary>
    private const double CurveStart = 0.04045;

    /// <summary>How much light a colour such as <c>#6b4dff</c> gives, from 0 for black to 1 for white.</summary>
    private static double Lightness(string hex)
    {
        double red = Channel(hex, 0), green = Channel(hex, 1), blue = Channel(hex, 2);
        return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    }

    /// <summary>One channel of a colour, red, green or blue, as the light it gives.</summary>
    private static double Channel(string hex, int at)
    {
        double value = int.Parse(hex.AsSpan(1 + at * 2, 2), NumberStyles.HexNumber) / 255.0;
        return value <= CurveStart ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}

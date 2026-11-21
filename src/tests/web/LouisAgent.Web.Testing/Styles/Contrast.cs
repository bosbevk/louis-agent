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

    /// <summary>What opens a colour Sass wrote in percentages, such as <c>rgb(76.9%, 73.5%, 100%)</c>.</summary>
    private const string InPercentages = "rgb(";

    /// <summary>
    /// How much light a colour gives, from 0 for black to 1 for white. It reads <c>#6b4dff</c>, and the
    /// <c>rgb(76.9%, 73.5%, 100%)</c> Sass writes for a mixed colour.
    /// </summary>
    private static double Lightness(string colour)
    {
        double[] light = [.. Channels(colour).Select(Light)];
        return 0.2126 * light[0] + 0.7152 * light[1] + 0.0722 * light[2];
    }

    /// <summary>A colour's red, green and blue, each from 0 to 1.</summary>
    private static IEnumerable<double> Channels(string colour) =>
        colour.StartsWith(InPercentages, StringComparison.Ordinal)
            ? colour[InPercentages.Length..].TrimEnd(')').Split(',').Select(Percentage)
            : Enumerable.Range(0, 3).Select(at => Hex(colour, at));

    /// <summary>One channel of a colour written as <c>#rrggbb</c>, from 0 to 1.</summary>
    private static double Hex(string colour, int at) =>
        int.Parse(colour.AsSpan(1 + at * 2, 2), NumberStyles.HexNumber) / 255.0;

    /// <summary>One channel written as a percentage, such as <c>76.9%</c>, from 0 to 1.</summary>
    private static double Percentage(string channel) =>
        double.Parse(channel.Trim().TrimEnd('%'), CultureInfo.InvariantCulture) / 100;

    /// <summary>One channel, from 0 to 1, as the light it gives: a screen's curve, by G18's sum.</summary>
    private static double Light(double value) =>
        value <= CurveStart ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
}

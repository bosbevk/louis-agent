using System.Xml.Linq;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// The louis-agent brand in a picture's footer (F164.1, Louis: "every image must have our branding", his pick "brand
/// in the footer"): the site's own wordmark, the name with the mark, the violet disc, standing in for its "o" (Louis:
/// "must look like this one just a bit smaller"), at the footer's right on the line the key's heading stands on.
/// </summary>
/// <remarks>
/// One small class the builder asks, so the mark's colour and the wordmark's letters are named once (ADR 0123):
/// every kind the builder puts together carries the same brand, and the ticket, which is a card in the chat and not a
/// picture, carries none (Louis: "No, pictures only"). The wordmark is one line of text, the mark Geist's own bullet
/// in it (F169.3, Louis's pick A of three, "Geist's own bullet, in the line"), so the browser sets the letters and
/// the mark together from the picture's own font. At 1.41 times the letters its disc is 0.41 of them, the site's
/// header's share, and a tenth of a letter down it stands on their line; the letters after it are lifted back.
/// The two picks it was weighed against: the whole name drawn as outlines, which never drifts but can't be read as
/// text or take the picture's colours, and a disc drawn apart by measured widths, which the engine can't do, since
/// it measures no font and a disc placed by a guess would drift off its letters. The disc character it replaces
/// was missing from Geist, so each reader's machine drew it from another font, too large and above the line.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal static class PictureBrand
{
    /// <summary>The mark's violet, the site's own: the flat theme's drawing of the mark is a disc of it.</summary>
    public const string Mark = "#6b4dff";

    /// <summary>The brand's name, as a screen reader hears it.</summary>
    public const string Name = "louis-agent";

    /// <summary>The room the footer takes when the picture has no key: its hairline and the brand's row.</summary>
    public const double Room = 34;

    /// <summary>Where the brand's words stand under the footer's hairline, as the key's heading does.</summary>
    public const double Line = 22;

    /// <summary>The wordmark's letters: the one before the mark, the mark, which stands for the "o", and the rest.</summary>
    private const string Before = "l", Disc = "•", After = "uis-agent";

    /// <summary>The wordmark's letters' size, in pixels.</summary>
    private const double Letters = 15;

    /// <summary>How much larger than the letters the bullet is set, so its disc is 0.41 of them.</summary>
    private const string MarkSize = "1.41em";

    /// <summary>How far the bullet is set down to stand on the line: a tenth of a letter.</summary>
    private const double Drop = Letters / 10;

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The brand, at the right of a picture that wide, on the row under a footer's hairline at its top.</summary>
    public static void Draw(SvgWriter writer, double width, double top)
    {
        // Text, the mark's span set down, and the rest's span lifted back: written with nothing between them, so the
        // name reads as one word and the letters after the mark keep the line.
        var mark = new XElement(Svg + "tspan", new XAttribute("class", "pic-mark"),
            new XAttribute("dy", SvgWriter.N(Drop)), Disc);
        var rest = new XElement(Svg + "tspan", new XAttribute("dy", SvgWriter.N(-Drop)), After);
        var wordmark = new XElement(Svg + "text", new XAttribute("class", "pic-name"),
            new XAttribute("x", SvgWriter.N(width)), new XAttribute("y", SvgWriter.N(top + Line)),
            new XAttribute("text-anchor", "end"), new XAttribute("aria-label", Name), Before, mark, rest);
        writer.Group("pic-brand", 0, 0, [wordmark]);
    }

    /// <summary>
    /// The brand's look, as the site's wordmark: the name heavy and set tight in the picture's own words' colour, and
    /// the mark the site's violet, its disc the header's share of the letters.
    /// </summary>
    public static string Style =>
        $".pic-name {{ fill: var(--text); font: 650 {SvgWriter.N(Letters)}px var(--pic-sans); " +
        "letter-spacing: -.035em; }\n" +
        $".pic-mark {{ fill: {Mark}; font-size: {MarkSize}; }}\n";
}

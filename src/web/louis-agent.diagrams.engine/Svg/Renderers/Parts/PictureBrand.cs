using System.Xml.Linq;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// The louis-agent brand in a picture's footer (F162.1, Louis: "every image must have our branding", his pick "brand
/// in the footer"): the site's own wordmark, the name with the mark, the violet disc, standing in for its "o" (Louis:
/// "must look like this one just a bit smaller"), at the footer's right on the line the key's heading stands on.
/// </summary>
/// <remarks>
/// One small class the builder asks, so the mark's colour and the wordmark's letters are named once (ADR 0123):
/// every kind the builder puts together carries the same brand, and the ticket, which is a card in the chat and not a
/// picture, carries none (Louis: "No, pictures only"). The wordmark is one line of text, the mark a disc character in
/// it, so the browser sets the letters and the mark together: the engine measures no font, and a disc drawn apart
/// would drift off its letters.
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
    private const string Before = "l", Disc = "●", After = "uis-agent";

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The brand, at the right of a picture that wide, on the row under a footer's hairline at its top.</summary>
    public static void Draw(SvgWriter writer, double width, double top)
    {
        // Text, the mark's span and text again: words and a span mixed, so nothing is written between them and the
        // name reads as one word.
        var wordmark = new XElement(Svg + "text", new XAttribute("class", "pic-name"),
            new XAttribute("x", SvgWriter.N(width)), new XAttribute("y", SvgWriter.N(top + Line)),
            new XAttribute("text-anchor", "end"), new XAttribute("aria-label", Name), Before,
            new XElement(Svg + "tspan", new XAttribute("class", "pic-mark"), Disc), After);
        writer.Group("pic-brand", 0, 0, [wordmark]);
    }

    /// <summary>
    /// The brand's look, as the site's wordmark: the name heavy and set tight in the picture's own words' colour, and
    /// the mark the site's violet, as big as a small letter, since a disc character draws smaller than its size.
    /// </summary>
    public static string Style =>
        ".pic-name { fill: var(--text); font: 650 15px var(--pic-sans); letter-spacing: -.035em; }\n" +
        $".pic-mark {{ fill: {Mark}; font-size: 1.2em; }}\n";
}

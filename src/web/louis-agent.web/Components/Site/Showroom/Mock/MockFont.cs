using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The faces a mocked business's website wears (F127.1, its sample brand): a display face for its wordmark and
/// headings, a text face and a face for small capitals. A serif, a slab, a clean sans or a rounded one are one face
/// each, from the fonts a browser already has; since F127.2 a brand can wear a world-class pairing, served by the
/// framework itself. Its wire name is its class, <c>font-serif</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MockFont
{
    [WireName("serif")] Serif,
    [WireName("slab")] Slab,
    [WireName("sans")] Sans,
    [WireName("rounded")] Rounded,

    /// <summary>A coastal boutique: Cormorant Garamond, with Jost for its text and capitals.</summary>
    [WireName("cormorant")] Cormorant,

    /// <summary>A farm bistro: Gloock, with Lora for its text and Josefin Sans for its capitals.</summary>
    [WireName("gloock")] Gloock,

    /// <summary>An established firm: Bodoni Moda, with Hanken Grotesk for its text and capitals.</summary>
    [WireName("bodoni")] Bodoni,
}

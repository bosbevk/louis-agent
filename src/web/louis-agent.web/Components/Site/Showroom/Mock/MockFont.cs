using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The typeface a mocked business's website wears (F127.1, its sample brand): a serif, a slab, a clean sans or a
/// rounded one, each from the fonts a browser already has, so a mock loads nothing. Its wire name is its class,
/// <c>font-serif</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MockFont
{
    [WireName("serif")] Serif,
    [WireName("slab")] Slab,
    [WireName("sans")] Sans,
    [WireName("rounded")] Rounded,
}

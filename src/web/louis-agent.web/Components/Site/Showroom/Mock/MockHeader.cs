using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The header a mocked business's website wears (F127.2, its sample brand, mockup 0): a band of its brand colour, a
/// page-coloured header under a rule in its label colour, or one over a double line in its brand colour. Its wire name
/// is its class, <c>head-band</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MockHeader
{
    /// <summary>A band of the brand colour, a hairline under it: a coastal boutique.</summary>
    [WireName("band")] Band,

    /// <summary>The page's colour under a rule of the label colour: an established firm.</summary>
    [WireName("ruled")] Ruled,

    /// <summary>The page's colour over a double line of the brand colour: a farm bistro.</summary>
    [WireName("lined")] Lined,
}

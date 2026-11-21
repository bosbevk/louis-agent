using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The look a mocked business's website has (F127.1, Louis: "give each one a different look"): its words beside its
/// scene, set out like a magazine, or under a wide banner of its scene. Its wire name is its class,
/// <c>look-split</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MockLook
{
    [WireName("split")] Split,
    [WireName("editorial")] Editorial,
    [WireName("banner")] Banner,
}

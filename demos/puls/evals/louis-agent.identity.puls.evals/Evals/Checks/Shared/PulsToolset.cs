using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls.Evals.Checks.Shared;

/// <summary>
/// The toolsets Puls's checks and tests name in a mode. A member's lower-case name is its wire name, or the one its
/// <c>[WireName]</c> gives, the one a run's protocol line and a session request carry.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum PulsToolset
{
    Files,
    Git,
    Web,
    [WireName("where-to-buy")] WhereToBuy,
    [WireName("shop-prices")] ShopPrices,
    Specialists,
    Minions,
    Skills,
    Recipes,
    Calculator,
}

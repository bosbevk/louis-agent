namespace LouisAgent.Packs.Shops.Checks.Reading;

/// <summary>
/// The shop the user chose, as a scenario names it under <c>shop</c>. It decides what a shop rule asks of the answer:
/// a named shop's branch under Where to buy, the missing branch said (<see cref="NoBranch"/>), or estimates alone
/// (<see cref="Any"/>).
/// </summary>
/// <remarks>
/// The pack's own copy of the names (ADR 0043: each pluggable project keeps its own enum of a repeated name), read once
/// from the scenario by <see cref="ScenarioShop"/> (ADR 0024).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ShopExpectation
{
    Checkers,
    Pnp,
    Woolworths,
    NoBranch,
    Any,
}

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;

/// <summary>
/// The shop the user chose for a cook conversation, as Puls's briefing names it (<c>Winkel</c> / <c>Shop</c>). It
/// decides how the cook should price: <see cref="Checkers"/> with its shop tool for the user's town,
/// <see cref="Pnp"/> or <see cref="Woolworths"/> from a page of the shop's site (ReadRenderedPage), and
/// <see cref="Any"/> (the user said it doesn't matter, or asked for the recipe only) not at all, with estimates
/// instead.
/// </summary>
/// <remarks>Parsed once from the scenario's <c>shop</c> (ADR 0024: choices are enums).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ShopExpectation
{
    Checkers,
    Pnp,
    Woolworths,
    NoBranch,
    Any,
}

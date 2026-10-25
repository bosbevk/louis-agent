namespace LouisAgent.Evals.Puls.Evals.Cook.Expectations;

/// <summary>
/// The shop the user chose for a cook conversation (F18 step 5.4; ADR 0024: parsed once from the scenario's <c>shop</c>), as Puls's briefing names it
/// (<c>Winkel</c> / <c>Shop</c>): <see cref="Checkers"/>, which the cook prices with its shop tool for the user's town, <see cref="Pnp"/> or <see cref="Woolworths"/>, which it prices from a page of the shop's site (ReadRenderedPage), or <see cref="Any"/> (<c>enige</c> /
/// <c>any</c>: the user said it doesn't matter, or asked for the recipe only), for which the cook prices nothing and writes estimates.
/// </summary>
public enum ShopExpectation
{
    Checkers,
    Pnp,
    Woolworths,
    NoBranch,
    Any,
}

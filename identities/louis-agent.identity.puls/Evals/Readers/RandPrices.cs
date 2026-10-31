using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Readers;

/// <summary>
/// Reads whether a cook's answer gives its prices as fact: the rows of its list priced from a shop or guessed (a rand
/// amount with no link and no ±), and the shops' sites its prices link to. An estimate (<c>± R65</c>) is neither.
/// </summary>
/// <remarks>
/// An Adapter (Gang of Four, Louis's pick, F49.3 step 3) over <see cref="Prices.Priced"/> and
/// <see cref="Prices.PriceLinkHosts"/>, the reading the cook's <c>estimates-at-shop</c> did before it moved to the
/// shops pack, unchanged, so the rule over this reader grades as it did. The eval file names it <c>rand-prices</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0059-an-identity-ships-readers-not-checks.md">ADR 0059: an identity ships readers, not checks</seealso>
public sealed class RandPrices : IPriceReader
{
    /// <inheritdoc />
    public string Name => "rand-prices";

    /// <inheritdoc />
    public int AsFact(string answer) => Prices.Priced(answer) is { } count ? count.Priced + count.Guessed : 0;

    /// <inheritdoc />
    public IReadOnlyList<string> LinkedSites(string answer) => Prices.PriceLinkHosts(answer);
}

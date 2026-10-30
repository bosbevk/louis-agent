using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Evals.Readers;

/// <summary>
/// Reads the rand total of the basket the cook priced: the line that says the total (Afrikaans or English), the bold
/// total row of an estimates list, or the linked price rows added up. An answer whose total it can't be sure of gives
/// none, never a guess.
/// </summary>
/// <remarks>
/// An Adapter (Gang of Four, Louis's pick) over <see cref="Prices.Total"/>, the cook's budget rule's own reading, so
/// <c>amount-under</c> over this reader grades as <c>within-budget</c> did. The eval file names it
/// <c>rand-total</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class RandTotal : IMoneyReader
{
    /// <inheritdoc />
    public string Name => "rand-total";

    /// <inheritdoc />
    public decimal? Total(string answer) => Prices.Total(answer)?.Amount;
}

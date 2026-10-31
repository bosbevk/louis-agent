namespace LouisAgent.Evals.Readers;

/// <summary>
/// Reads whether an answer gives prices as fact, where it should give estimates: how many of its list's rows state a
/// price, and which shops' sites its prices link to.
/// </summary>
/// <remarks>
/// The two questions a shop rule asks of an answer's prices (Louis's pick, F49.3 step 3): an identity's reader knows
/// how its agents write a price (a currency, an estimate's mark), the rule knows what it may not hold.
/// </remarks>
public interface IPriceReader : IReader
{
    /// <summary>
    /// How many rows of the answer's list state a price as fact: priced from a shop, or an amount with no source that
    /// isn't marked an estimate.
    /// </summary>
    int AsFact(string answer);

    /// <summary>The sites of the shops the list's prices link to, each once, in the order they first appear.</summary>
    IReadOnlyList<string> LinkedSites(string answer);
}

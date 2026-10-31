using LouisAgent.Evals.Readers;
using LouisAgent.Identities.Puls.Evals.Readers;

namespace LouisAgent.Identities.Puls.Evals;

/// <summary>
/// Puls's own readers, the ones its eval files name beside a check: what a cook's answer says a basket costs, whether
/// it gives prices as fact, how many people a meal is for, and which of its images are photos of the dish.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), as <c>PulsChecks</c> is for checks: Puls lists its readers in one place, and the
/// composition's <c>EvalReaders</c> gathers them for the runner, so the runner never names Puls.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class PulsReaders
{
    /// <summary>Every reader of Puls's, by name.</summary>
    public static IReadOnlyList<IReader> All { get; } =
        [new RandTotal(), new RandPrices(), new PeopleServed(), new RecipePhotos()];
}

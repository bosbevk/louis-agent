using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Identities.Puls.Evals;
using LouisAgent.Identities.Puls.Judges;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;

namespace LouisAgent.PulsEvalRunner;

/// <summary>
/// What Puls's evals can name beyond the harness's general checks: each pack's checks, then Puls's judges, and Puls's
/// readers.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): this is where the lists meet, in Puls's own program, so no framework project and no host
/// ever carries a check (ADR 0073). It's a class and not a line in Program.cs so Puls's tests load the same lists.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0073-packs-reference-no-evals.md">ADR 0073: packs reference no evals</seealso>
public static class PulsCatalogs
{
    /// <summary>The lists Puls's program hands to the runner.</summary>
    public static RunnerCatalogs All { get; } = new(
        Checks: [.. WhereToBuyChecks.All, .. RecipesChecks.All, .. PulsJudges.All],
        Readers: [.. PulsReaders.All]);
}

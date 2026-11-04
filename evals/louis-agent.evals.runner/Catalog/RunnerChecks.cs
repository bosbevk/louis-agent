using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Identities.Puls.Judges;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// The check catalogue the runner gathers from every owner's list: the harness's general checks, then each pack's
/// checks project, then Puls's judges. An eval file can name any of them, and a type listed twice stops the runner's
/// start.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA). The runner is the one project that knows every evals project, as host-packs is for
/// identities and packs (ADR 0027), so it is where the lists meet. These projects sit under <c>evals/</c> and only the
/// runner references them, so no host ever carries a check (ADR 0073).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0073-packs-reference-no-evals.md">ADR 0073: packs reference no evals</seealso>
public static class RunnerChecks
{
    /// <summary>Every check type the runner's evals can name.</summary>
    public static CheckCatalog Catalog { get; } = new(
        [.. GeneralChecks.All, .. WhereToBuyChecks.All, .. RecipesChecks.All, .. PulsJudges.All]);
}

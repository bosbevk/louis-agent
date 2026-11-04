using LouisAgent.Evals.Catalog;

namespace LouisAgent.Packs.Recipes.Checks;

/// <summary>
/// The recipes pack's own checks, the ones that prove what its tools do, which an eval file can name: the list the
/// pack puts into the check catalogue, as it lists its tools for the agent.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), as <c>WhereToBuyChecks</c> is for its pack (Louis's pick, F49.3 finding K): each owner
/// lists its entries in one place, and the composition's <c>EvalChecks</c> gathers the lists, so the runner never names
/// a pack. The pack references the check contract for them, never the harness.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0060-a-pack-ships-its-checks.md">ADR 0060: a pack ships its checks</seealso>
public static class RecipesChecks
{
    /// <summary>Every check of the pack's, in the order an eval lists them.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [new RecipeFromSource.Entry()];
}

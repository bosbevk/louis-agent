using LouisAgent.Evals.Catalog;

namespace LouisAgent.Packs.Shops.Checks;

/// <summary>
/// The shops pack's own checks, the ones that prove what its tools do, which an eval file can name: the list the
/// pack puts into the check catalogue, as it lists its tools for the agent.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), as <c>PulsChecks</c> and <c>PulsReaders</c> are (Louis's pick, F49.3 finding K): each
/// owner lists its entries in one place, and the composition's <c>EvalChecks</c> gathers the lists, so the runner
/// never names a pack. The pack references the check contract for them, never the harness.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class ShopsChecks
{
    /// <summary>Every check of the pack's, in the order the cook's eval lists them.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [new EstimatesAtShop.Entry(), new UntooledShops.Entry()];
}

using LouisAgent.Evals.Catalog;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

namespace LouisAgent.Composition;

/// <summary>
/// Every identity's and pack's own checks that an eval file can name, which the runner gathers with the harness's
/// general checks. Today that is Puls's fixed rules.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): each owner lists its checks in one place, and this is where the lists meet, since the
/// composition is the one project that knows every identity and pack, so the runner never names one. A judge that
/// needs the harness is not here: the composition, which every host loads, must never carry the harness.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class EvalChecks
{
    /// <summary>Every identity's and pack's check types, an identity's in the order its list gives.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [.. PulsChecks.All];
}

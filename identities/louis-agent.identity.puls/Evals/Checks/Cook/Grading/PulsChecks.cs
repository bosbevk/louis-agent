using LouisAgent.Evals.Catalog;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

/// <summary>
/// Puls's own fixed rules that an eval file can name: the list Puls puts into the check catalogue, which the runner
/// gathers with the harness's general checks and Puls's judges. Empty since F49.3 moved the cook's shop rules to the
/// shops pack; the rest of the cook's rules are general checks over Puls's readers (<c>PulsReaders</c>).
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): each owner lists its entries in one place, and the runner gathers the lists through the
/// composition's <c>EvalChecks</c>. Puls's judge is in <c>PulsJudges</c>, in a project of its own, since it needs the
/// harness. Step 4 of F49.3 decides whether the empty list stays or goes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class PulsChecks
{
    /// <summary>Every fixed rule of Puls's: none now.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [];
}

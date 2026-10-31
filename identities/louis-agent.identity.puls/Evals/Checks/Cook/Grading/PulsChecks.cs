using LouisAgent.Evals.Catalog;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

/// <summary>
/// Puls's own fixed rules that an eval file can name: the list Puls puts into the check catalogue, which the runner
/// gathers with the harness's general checks and Puls's judges. Today that is the cook's two shop rules; the rest of
/// the cook's rules are general checks over Puls's readers (<c>PulsReaders</c>).
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): each owner lists its entries in one place, and the runner gathers the lists through the
/// composition's <c>EvalChecks</c>. Puls's judge is in <c>PulsJudges</c>, in a project of its own, since it needs the
/// harness; the shops' two checks move to the shops pack in F49.3.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class PulsChecks
{
    /// <summary>Every fixed rule of Puls's, in the order the cook's eval lists its checks.</summary>
    public static IReadOnlyList<ICheckType> All { get; } = [new EstimatesAtShop.Entry(), new UntooledShops.Entry()];
}

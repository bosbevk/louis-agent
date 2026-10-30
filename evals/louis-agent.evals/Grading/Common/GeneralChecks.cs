using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common.Answers;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The harness's own check types, the ones any identity's eval can name from a file: the list the harness puts into
/// the catalogue.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): each owner lists its entries in one place, and the composition gathers the lists; this
/// is the harness's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class GeneralChecks
{
    /// <summary>Every general check type, in the order the catalogue lists them.</summary>
    public static IReadOnlyList<ICheckType> All { get; } =
        [new NoErrorCheck.Entry(), new WithinTimeCheck.Entry(), new CostUnderCheck.Entry(), new ContainsCheck.Entry()];
}

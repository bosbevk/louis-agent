using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.WhereToBuy;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// Puls's own check types, the cook's fixed rules, that an eval file can name: the list Puls puts into the check
/// catalogue, which the runner gathers with the harness's general checks.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): each owner lists its entries in one place, and the runner gathers the lists. The list
/// moves beside the identity with its checks in F49.2 step 9, and the shops' two to the shops pack in F49.3. The
/// judge, <c>meal-for-people</c>, joins in step 8.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class PulsChecks
{
    /// <summary>Every check type of Puls's, in the order the cook's eval lists its checks.</summary>
    public static IReadOnlyList<ICheckType> All { get; } =
    [
        new CookNoError.Entry(), new WithinBudget.Entry(), new EstimatesAtShop.Entry(), new UntooledShops.Entry(),
        new RecipeImage.Entry(), new ServesAsAsked.Entry()
    ];
}

using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common.Answers;
using LouisAgent.Evals.Grading.Common.Compare;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Grading.Common.Similarity;
using LouisAgent.Evals.Grading.Common.Tools;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Models.Rubrics;

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
    /// <summary>
    /// Every general check type, by family, in the order the catalogue lists them: the run's facts, the text rules,
    /// the structure, the tool calls, the compares (through a reader, or to a link), the similarity, then the model and
    /// the person.
    /// </summary>
    public static IReadOnlyList<ICheckType> All { get; } =
    [
        new NoErrorCheck.Entry(), new WithinTimeCheck.Entry(), new CostUnderCheck.Entry(), new TurnsUnderCheck.Entry(),
        new TokensUnderCheck.Entry(),
        new ContainsCheck.Entry(), new NotContainsCheck.Entry(), new StartsWithCheck.Entry(),
        new LengthUnderCheck.Entry(), new MentionsExpectedCheck.Entry(),
        new IsJsonCheck.Entry(),
        new RoutedToCheck.Entry(), new ToolUsedCheck.Entry(), new ToolNotUsedCheck.Entry(),
        new ToolUsedWithCheck.Entry(),
        new ToolCallsUnderCheck.Entry(),
        new AmountUnderCheck.Entry(), new NumberNamedCheck.Entry(), new ImageShownCheck.Entry(),
        new LinksToHostCheck.Entry(),
        new NearCheck.Entry(),
        new JudgeCheck.Entry(), new HumanReviewCheck.Entry()
    ];
}

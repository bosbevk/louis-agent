using LouisAgent.Puls.Admin.Grades;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Grades;

/// <summary>A conversation's outcome and its grade chips (<see cref="GradeChipList"/>), in the run's grader order.</summary>
public partial class GradeChips
{
    [Parameter, EditorRequired] public ConversationGrades Grades { get; set; } = ConversationGrades.None;

    /// <summary>The run's graders, for the chips' order; null for a run from before F18 step 2a.</summary>
    [Parameter] public IReadOnlyList<GraderRow>? Order { get; set; }

    private IReadOnlyList<GradeChip> Chips => GradeChipList.Of(Grades, Order);

    private string? OutcomeKind => Grades.Outcome is { } outcome ? GradeOutcomes.KindOf(outcome) : null;
}

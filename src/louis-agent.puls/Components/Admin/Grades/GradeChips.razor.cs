using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Chips;
using LouisAgent.Puls.Admin.Reviews;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Grades;

/// <summary>A conversation's outcome and its grade chips (<see cref="GradeChipList"/>), in the run's grader order; a judge with a human review is blind until this reviewer has graded (<see cref="BlindReview"/>).</summary>
public partial class GradeChips : IDisposable
{
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    [Parameter, EditorRequired] public ConversationGrades Grades { get; set; } = ConversationGrades.None;

    /// <summary>The run's graders, for the chips' order; null for a run from before F18 step 2a.</summary>
    [Parameter]
    public IReadOnlyList<GraderRow>? Order { get; set; }

    /// <summary>Which conversation this is, for the judge's chip to know whether this reviewer graded it; null where the chips are not of one conversation.</summary>
    [Parameter]
    public ConversationRef? Conversation { get; set; }

    private IReadOnlyList<GradeChip> Chips => GradeChipList.Of(Grades, Order, Hidden, Reviewer.Name);

    private IReadOnlySet<string> Hidden => BlindReview.HiddenJudges(Grades, Order, Reviewer.Name,
        judge => Conversation is not null && Reviewer.Graded(Conversation, judge));

    /// <summary>The conversation's roll-up outcome, shown as it is stored.</summary>
    private string? OutcomeKind => Grades.Outcome is { } outcome ? ChipKind.Of(outcome) : null;

    protected override async Task OnInitializedAsync()
    {
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Reviewer.Changed -= Redraw;
}

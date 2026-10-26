using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>A conversation's review state for this reviewer, as a pill (<see cref="ReviewStatus"/>); it follows <see cref="ReviewerState"/> so a grade given this session shows at once.</summary>
public partial class ReviewPill : IDisposable
{
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    [Parameter, EditorRequired] public ConversationGrades Grades { get; set; } = ConversationGrades.None;
    [Parameter] public IReadOnlyList<GraderRow>? Order { get; set; }
    [Parameter, EditorRequired] public ConversationRef Conversation { get; set; } = null!;

    private ReviewState State => ReviewStatus.Of(Grades, Order, Reviewer.Name, judge => Reviewer.Graded(Conversation, judge));

    protected override async Task OnInitializedAsync()
    {
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Reviewer.Changed -= Redraw;
}

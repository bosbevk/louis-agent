using System.Net.Http.Json;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>
/// A conversation's blind review (F18 step 6a.3): the question, what was asked, the answer, pass / fail / unsure and a note. The judge's verdict and reason are not drawn until this reviewer
/// has graded it (<see cref="BlindReview"/>), then they are, with whether the person agreed.
/// </summary>
public partial class ReviewPanel : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    [Parameter, EditorRequired] public ConversationRef Conversation { get; set; } = null!;
    [Parameter, EditorRequired] public GraderRow Review { get; set; } = null!;
    [Parameter] public IReadOnlyList<string> Asked { get; set; } = [];
    [Parameter] public string? Answer { get; set; }
    [Parameter] public ConversationGrades Grades { get; set; } = ConversationGrades.None;

    private string _note = "";
    private string _typedName = "";
    private string? _verdict;
    private string? _error;
    private bool _busy;

    private string Judge => BlindReview.JudgeOf(Review.Name);

    private string QuestionText => Review.Question ?? $"Is this answer good enough for {Judge}?";

    private bool HasName => Reviewer.Name is not null;

    private bool JudgeShown => !BlindReview.HiddenJudges(Grades, [Review], Reviewer.Name, j => Reviewer.Graded(Conversation, j)).Contains(Judge);

    private GradeRow? JudgeGrade => Grades.Grades?.FirstOrDefault(g => g.Grader == Judge);

    /// <summary>What this reviewer said: just now, else their latest grade stored before.</summary>
    private string? PersonVerdict => _verdict ?? BlindReview.OwnGrades(Grades, Judge, Reviewer.Name).OrderByDescending(h => h.At).Select(h => h.Verdict).FirstOrDefault();

    /// <summary>The segmented group's lit class on the button of the verdict this reviewer gave.</summary>
    private string Pressed(string verdict) => PersonVerdict == verdict ? "active" : "";

    /// <summary>aria-checked wants the words, and Blazor writes a bool as an empty attribute.</summary>
    private string Checked(string verdict) => PersonVerdict == verdict ? "true" : "false";

    private string ResultText => PersonVerdict is { } person && JudgeGrade is { } judge ? BlindReview.Compare(person, judge.Outcome) switch
    {
        ReviewVerdictKind.Agreed => "You agreed with the judge.",
        ReviewVerdictKind.Disagreed => "You disagreed with the judge.",
        ReviewVerdictKind.YouWereUnsure => "You were unsure.",
        _ => "The judge could not tell.",
    } : "";

    private string JudgeText => JudgeGrade?.Outcome switch
    {
        GradeOutcome.Pass => "The judge said pass.",
        GradeOutcome.Fail => "The judge said fail.",
        _ => "The judge could not tell.",
    };

    protected override async Task OnInitializedAsync()
    {
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    private async Task KeepNameAsync()
    {
        await Reviewer.SetAsync(_typedName);
        _typedName = Reviewer.Name ?? _typedName;
    }

    /// <summary>"Not you?": lets the kept name go, so the panel asks for one again (6a.4).</summary>
    private Task ChangeNameAsync() => Reviewer.ClearAsync();

    private async Task GradeAsync(string verdict)
    {
        if (Reviewer.Name is not { } name || _busy) return;
        _busy = true;
        _error = null;
        try
        {
            string path = string.Join('/', new[] { Conversation.Run, "reviews", Conversation.Mode, Conversation.Repeat.ToString(), Conversation.Task, Review.Name }.Select(Uri.EscapeDataString));
            using var request = new HttpRequestMessage(HttpMethod.Put, $"v1/admin/benchmarks/runs/{path}") { Content = JsonContent.Create(new { verdict, note = _note, reviewer = name }) };
            using var _ = await Api.SendAsync(request);
            _verdict = verdict;
            Reviewer.Record(Conversation, Judge);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose() => Reviewer.Changed -= Redraw;
}

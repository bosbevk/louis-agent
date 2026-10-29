using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Conversations;

/// <summary>
/// The conversations of every mode side by side, a card each with its review state, chips and cost, filtered to
/// those this reviewer still has to grade when asked; their tokens under them.
/// </summary>
public partial class ConversationsLevel
{
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The run's id, for each conversation's key.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The repeat shown, for each conversation's key.</summary>
    [Parameter, EditorRequired]
    public int Repeat { get; set; }

    /// <summary>The run's graders, for the order of the grade chips; none for a run from before them.</summary>
    [Parameter]
    public IReadOnlyList<GraderRow>? Graders { get; set; }

    /// <summary>Only the conversations still waiting for this reviewer.</summary>
    [Parameter]
    public bool OnlyToReview { get; set; }

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone switched the filter.</summary>
    [Parameter]
    public EventCallback<bool> OnFilter { get; set; }

    /// <summary>Someone wants the chats side by side on a conversation.</summary>
    [Parameter]
    public EventCallback<string> OnReplay { get; set; }

    /// <summary>Someone opened a conversation's turns.</summary>
    [Parameter]
    public EventCallback<string> OnOpenConversation { get; set; }

    /// <summary>The tasks the filter keeps: all of them, or those any mode still has to review.</summary>
    private List<string> Tasks =>
        Tree.Tasks.Where(t => ReviewStatus.Keeps(OnlyToReview, Tree.Modes.Select(m => StatusOf(m, t)))).ToList();

    /// <summary>A mode's conversation of a task, as its review stands; no review when the mode hasn't it.</summary>
    private ReviewState StatusOf(ModeNode m, string task) =>
        m.Conversation(task) is { } c ? Status(c, m) : ReviewState.NoReview;

    /// <summary>Where a conversation stands for this reviewer: waiting for a grade, or graded.</summary>
    private ReviewState Status(ConversationNode c, ModeNode m) =>
        ReviewStatus.Of(c.Grades, Graders, Reviewer.Name, judge => Reviewer.Graded(Where(c, m), judge));

    /// <summary>The conversation's key in this run and repeat.</summary>
    private ConversationRef Where(ConversationNode c, ModeNode m) => new(RunId, m.Name, Repeat, c.Task);
}

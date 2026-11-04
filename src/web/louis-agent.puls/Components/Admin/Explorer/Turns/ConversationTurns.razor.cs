using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Turns;

/// <summary>
/// One conversation in every mode: its turns with their requests on a timeline, the review waiting on it, and its
/// notes. A turn opens its requests.
/// </summary>
public partial class ConversationTurns
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The conversation's task.</summary>
    [Parameter, EditorRequired]
    public string TaskId { get; set; } = "";

    /// <summary>The run's id, for the conversation's key.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The repeat shown, for the conversation's key.</summary>
    [Parameter, EditorRequired]
    public int Repeat { get; set; }

    /// <summary>The run's graders, to find the review.</summary>
    [Parameter]
    public IReadOnlyList<GraderRow>? Graders { get; set; }

    /// <summary>The run's notes, as the page last fetched them.</summary>
    [Parameter]
    public IReadOnlyList<RunNoteEntry> Notes { get; set; } = [];

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone wants the chats side by side on the conversation.</summary>
    [Parameter]
    public EventCallback<string> OnReplay { get; set; }

    /// <summary>Someone opened a turn's requests.</summary>
    [Parameter]
    public EventCallback<int> OnOpenTurn { get; set; }

    /// <summary>What a mode's column says: the conversation's turns, requests and cost, or that it hasn't it.</summary>
    private string Heading(ConversationNode? c) =>
        c is null ? "not in this mode" : $"{c.Turns.Count} turns · {c.Rows.Count} requests · {Money(c.Cost)}";

    /// <summary>The conversation's key in a mode.</summary>
    private ConversationRef Where(ModeNode m) => new(RunId, m.Name, Repeat, TaskId);

    /// <summary>A cost in the chosen currency, or a dash.</summary>
    private string Money(decimal? usd) => ExplorerText.Money(usd, Currency);
}

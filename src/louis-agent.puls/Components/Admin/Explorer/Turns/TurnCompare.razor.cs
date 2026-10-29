using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Turns;

/// <summary>
/// One turn in every mode: its requests on a timeline and its transcript, which you can replay as it was typed. The
/// explorer runs the replay, so leaving the turn stops it.
/// </summary>
public partial class TurnCompare
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The conversation's task.</summary>
    [Parameter, EditorRequired]
    public string TaskId { get; set; } = "";

    /// <summary>The turn's number.</summary>
    [Parameter, EditorRequired]
    public int TurnNumber { get; set; }

    /// <summary>The turns being replayed, by mode, filling in as their events play.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, Turn> Replays { get; set; } = new Dictionary<string, Turn>();

    /// <summary>A replay is running.</summary>
    [Parameter]
    public bool Replaying { get; set; }

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone wants the turn replayed as typed.</summary>
    [Parameter]
    public EventCallback OnReplay { get; set; }

    /// <summary>Someone stopped the replay.</summary>
    [Parameter]
    public EventCallback OnStop { get; set; }

    /// <summary>Someone opened a request, of this mode.</summary>
    [Parameter]
    public EventCallback<(RequestNode Request, ModeNode Mode)> OnOpenRequest { get; set; }

    /// <summary>The turn in a mode, if the mode has it.</summary>
    private TurnNode? TurnIn(ModeNode m) => m.Conversation(TaskId)?.Turns.FirstOrDefault(t => t.N == TurnNumber);

    /// <summary>What a mode's column says of the turn: its cost, requests and time, or that it hasn't it.</summary>
    private string Heading(TurnNode? tu)
    {
        if (tu is null) return "no turn in this mode";

        string cost = ExplorerText.Money(tu.Cost, Currency);
        return $"{cost} · {tu.Rows.Count} requests · {ExplorerText.Secs(tu.Seconds)} s";
    }
}

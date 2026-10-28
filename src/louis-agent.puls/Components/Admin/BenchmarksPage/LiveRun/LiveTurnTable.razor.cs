using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.LiveRuns;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.LiveRun;

/// <summary>
/// The run in progress as a table: a row per turn (its mode, conversation and question, who answered, its calls, its
/// time and its cost), each conversation's score chips under its turns, and "typing…" under a turn still going.
/// </summary>
public partial class LiveTurnTable
{
    /// <summary>The turns, their chips, their costs and the open one.</summary>
    [Parameter, EditorRequired]
    public LiveTurns Turns { get; set; } = null!;

    /// <summary>The run: its repeats name a row's repeat, and while it runs an open turn says it's typing.</summary>
    [Parameter, EditorRequired]
    public LiveRunRow Run { get; set; } = null!;

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>A row was clicked: open its turn.</summary>
    [Parameter]
    public EventCallback<TurnKey> OnOpen { get; set; }
}

using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.RunPage;

/// <summary>
/// The run page's head: which check, what it proves, and the run's facts (status, start, repeats, cost, the run it
/// re-scored), read from the contract's run row.
/// </summary>
public partial class RunHead
{
    /// <summary>The run's id, shown even before the run is read.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The run, once it is read.</summary>
    [Parameter]
    public StoredRunRow? Run { get; set; }

    /// <summary>The check's name, once the run is read.</summary>
    [Parameter]
    public string? Check { get; set; }

    /// <summary>What the check proves, when the list of checks names it.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The run's status as running, done or failed.</summary>
    private string Status => Run?.Status switch
    {
        "running" => "running",
        "done" => "done",
        _ => "failed"
    };

    /// <summary>When the run started, in the reader's own time.</summary>
    private string Started => Run!.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>How many repeats, as words: "1 repeat" or "3 repeats".</summary>
    private string Repeats => Run!.Repeat == 1 ? "1 repeat" : $"{Run.Repeat} repeats";

    /// <summary>
    /// The run's cost and how many requests it made, when it has a total; a dash for a cost nobody priced.
    /// </summary>
    private string? Total
    {
        get
        {
            if (Run?.Total is not { } total) return null;
            string cost = total.Cost is { } c ? UsageFormat.Money(c, "USD", Currency) : "—";
            return $"{cost}, {total.Requests} requests";
        }
    }
}

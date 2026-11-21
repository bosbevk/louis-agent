using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Head;

/// <summary>
/// The eval studio's card on the admin's home (F144): how many evals the runner has, how many answers wait for this
/// reviewer's grade, and which run was last, with the way to it.
/// </summary>
/// <remarks>
/// It reads the list the studio's page reads and the count the sidebar's badge shows, through the same
/// <see cref="ReviewQueueState"/>, so the two numbers agree (ADR 0042); a refused key, a saved one and a lost API are
/// <see cref="AdminCardBase"/>'s to handle.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class EvalsCard
{
    private EvalListReply? _list;

    [Inject] private ReviewerState Reviewer { get; set; } = null!;
    [Inject] private ReviewQueueState Queue { get; set; } = null!;

    /// <summary>The last run of any eval: which eval, and the run.</summary>
    private sealed record LatestRun(string Eval, StoredRunRow Run);

    /// <summary>Listens for the count, then reads as every card does.</summary>
    protected override Task OnInitializedAsync()
    {
        Queue.Changed += Redraw;
        return base.OnInitializedAsync();
    }

    /// <summary>The evals, then the count waiting for a grade.</summary>
    protected override async Task ReadAsync()
    {
        _list = await Api.GetAsync<EvalListReply>("v1/admin/evals", AdminJson.SnakeCase);
        await Queue.ReadOnceAsync(Api, Reviewer);
    }

    /// <summary>Stops listening, since the count outlives this card.</summary>
    public override void Dispose()
    {
        Queue.Changed -= Redraw;
        base.Dispose();
    }

    /// <summary>The newest run of any eval, or none when none has run.</summary>
    private static LatestRun? LastRun(EvalListReply list) => list.Evals
        .SelectMany(eval => eval.Runs.Select(run => new LatestRun(eval.Name, run.Run)))
        .MaxBy(latest => latest.Run.StartedAt);

    /// <summary>The count changed elsewhere, as the Review page grades.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}

using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Head;

/// <summary>
/// The eval studio's card on the admin's home (F144): how many evals the runner has, how many answers wait for this
/// reviewer's grade, and which run was last, with the way to it.
/// </summary>
/// <remarks>
/// It reads the list the studio's page reads and the count the sidebar's badge shows, through the same
/// <see cref="ReviewQueueState"/>, so the two numbers agree (ADR 0042). When the API says no, the card stays quiet
/// and the home asks for the key, as the other areas' cards do.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class EvalsCard : IDisposable
{
    private EvalListReply? _list;
    private bool _notRead;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;
    [Inject] private ReviewQueueState Queue { get; set; } = null!;

    /// <summary>The last run of any eval: which eval, and the run.</summary>
    private sealed record LatestRun(string Eval, StoredRunRow Run);

    /// <summary>Loads the admin key, then the evals and the count waiting for a grade.</summary>
    protected override async Task OnInitializedAsync()
    {
        Queue.Changed += Redraw;
        if (!Store.Loaded) await Store.LoadAsync();
        try
        {
            _list = await Api.GetAsync<EvalListReply>("v1/admin/evals", AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is AgentApiException or HttpRequestException)
        {
            _notRead = true;
            return;
        }

        await Queue.ReadOnceAsync(Api, Reviewer);
    }

    /// <summary>Stops listening, since the count outlives this card.</summary>
    public void Dispose()
    {
        Queue.Changed -= Redraw;
        GC.SuppressFinalize(this);
    }

    /// <summary>The newest run of any eval, or none when none has run.</summary>
    private static LatestRun? LastRun(EvalListReply list) => list.Evals
        .SelectMany(eval => eval.Runs.Select(run => new LatestRun(eval.Name, run.Run)))
        .MaxBy(latest => latest.Run.StartedAt);

    /// <summary>The count changed elsewhere, as the Review page grades.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}

using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Turns;
using LouisAgent.Composition.Messages;
using LouisAgent.Api.Evals.Messages;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Runs.Store;
using LouisAgent.Evals.Sessions;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Evals;

/// <summary>
/// The work of one run, as the desk runs it: the agent asked over the sessions API at the program's own address, or for
/// a re-grade no agent at all, the judge on its model, and the run kept in its file after every conversation. A run
/// that fails part way is kept as could not finish with the reason, and the failure goes on to the desk, which logs it.
/// </summary>
/// <param name="store">Where runs are kept.</param>
/// <param name="judgeModel">The model the judge marks criteria with.</param>
/// <param name="agentApi">A client of the API the agent is asked over, with its address and key.</param>
/// <param name="recordings">
/// Where a live play keeps the page its conversation names, beside the judge's answers, and a replay reads it back;
/// none when the host keeps no recordings.
/// </param>
/// <param name="clock">The time a run ends at.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class RunWork(
    RunStore store,
    IChatClient judgeModel,
    Func<HttpClient> agentApi,
    ReplayStore? recordings,
    TimeProvider clock)
{
    /// <summary>Where this host keeps its runs.</summary>
    public RunStore Store => store;

    /// <summary>
    /// The work that plays this run of this eval file to its end; a replay with the live run it plays, whose flips it
    /// keeps where the agent answered the same.
    /// </summary>
    public Func<RunSignals, Task> For(EvalRun started, EvalFile file, EvalRun? replayed = null) =>
        signals => KeepAsync(started, signals, (kept, playing) => PlayAsync(started, file, replayed, kept, playing));

    /// <summary>
    /// The work that re-grades a kept run's answers against this eval file, asking no agent (F126.4); it is kept,
    /// stopped and failed as a run is.
    /// </summary>
    public Func<RunSignals, Task> ForRegrade(EvalRun started, EvalRun graded, EvalFile file) =>
        signals => KeepAsync(started, signals, (kept, playing) =>
            new Regrader(new TurnGrader(new Judge(judgeModel)), clock)
                .RegradeAsync(started, graded, file, kept, playing));

    /// <summary>The run played over the sessions API, the agent asked at the program's own address.</summary>
    private async Task PlayAsync(EvalRun started, EvalFile file, EvalRun? replayed, Func<EvalRun, Task> kept,
        CancellationToken playing)
    {
        using var http = agentApi();
        // The page a conversation names is asked of the same API and kept with the judge's answers (F126.3.3).
        var pages = new PageClient(http, recordings);
        var player = new ConversationPlayer(new SessionClient(http), pages, new Judge(judgeModel), clock);
        await new EvalRunner(player, clock).RunAsync(started, file, kept, replayed, playing);
    }

    /// <summary>
    /// A run's work, kept as it goes: as stopped when a person stopped it, as could not finish when something failed,
    /// and left as it was when the host shut down, so it then reads as cut off.
    /// </summary>
    private async Task KeepAsync(EvalRun started, RunSignals signals,
        Func<Func<EvalRun, Task>, CancellationToken, Task> work)
    {
        // The run as last kept, so a failure keeps what was graded before it.
        var last = started;
        store.Save(started);
        try
        {
            await work(run =>
            {
                last = run;
                store.Save(run);
                return Task.CompletedTask;
            }, signals.Playing);
        }
        catch (OperationCanceledException) when (signals.Stopped.IsCancellationRequested)
        {
            // A person's stop is no failure: the run is kept as stopped with what was graded before it.
            var stoppedByAPerson = last with
            {
                Status = RunStatus.Stopped,
                EndedAt = clock.GetUtcNow(),
                StoppedBecause = MessageText.Format(EvalsApiMessage.StoppedByAPerson)
            };
            store.Save(stoppedByAPerson);
        }
        catch (Exception failed) when (failed is not OperationCanceledException)
        {
            var stopped = last with
            {
                Status = RunStatus.CouldNotFinish,
                EndedAt = clock.GetUtcNow(),
                StoppedBecause = failed.Message
            };
            store.Save(stopped);
            throw;
        }
    }
}

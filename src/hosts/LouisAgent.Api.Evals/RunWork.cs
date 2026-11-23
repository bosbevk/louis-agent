using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Runs.Store;
using LouisAgent.Evals.Sessions;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Evals;

/// <summary>
/// The work of one run, as the desk runs it: the agent asked over the sessions API at the program's own address,
/// the judge on its model, and the run kept in its file after every conversation. A run that fails part way is kept
/// as could not finish with the reason, and the failure goes on to the desk, which logs it.
/// </summary>
/// <param name="store">Where runs are kept.</param>
/// <param name="judgeModel">The model the judge marks criteria with.</param>
/// <param name="agentApi">A client of the API the agent is asked over, with its address and key.</param>
/// <param name="clock">The time a run ends at.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class RunWork(RunStore store, IChatClient judgeModel, Func<HttpClient> agentApi, TimeProvider clock)
{
    /// <summary>Where this host keeps its runs.</summary>
    public RunStore Store => store;

    /// <summary>The work that plays this run of this eval file to its end.</summary>
    public Func<CancellationToken, Task> For(EvalRun started, EvalFile file) =>
        cancellationToken => PlayAsync(started, file, cancellationToken);

    /// <summary>The run played and kept, or kept as could not finish when something failed.</summary>
    private async Task PlayAsync(EvalRun started, EvalFile file, CancellationToken cancellationToken)
    {
        // The run as last kept, so a failure keeps what was graded before it.
        var last = started;
        store.Save(started);

        using var http = agentApi();
        var player = new ConversationPlayer(new SessionClient(http), new Judge(judgeModel), clock);
        try
        {
            await new EvalRunner(player, clock).RunAsync(started, file, run =>
            {
                last = run;
                store.Save(run);
                return Task.CompletedTask;
            }, cancellationToken);
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

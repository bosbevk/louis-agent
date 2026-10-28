using LouisAgent.Api.Evals.Store.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Evals.Protocol.Events.Run;

namespace LouisAgent.Api.Evals.Store.Deletion;

/// <summary>
/// Clears out what an older build stored, when the store opens: every run written under an older protocol version,
/// or none, goes through the delete button's path with everything it kept and everything people said about it. It
/// logs one line with how much of each went, and nothing when there was nothing.
/// </summary>
/// <remarks>
/// MongoDB's Schema Versioning pattern, with the old versions thrown away instead of migrated: each run carries the
/// version of the protocol it was written under, and a run of an older one is deleted, never read (ADR 0049; 7c's
/// pick E). It deletes through <see cref="RunDeletion"/>, so it takes exactly what the delete button takes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public sealed class OldRunsCleanup(IRunStore runs, RunDeletion deletion, ILogger log)
{
    /// <summary>Deletes every run from before this protocol version, and says how much of each went.</summary>
    public async Task<DeletedRun> RunAsync(CancellationToken cancellationToken = default)
    {
        // Each old run goes the way the delete button takes it, and its counts add up.
        var total = new DeletedRun(0, 0, 0, 0, 0);
        foreach (string id in await runs.RunsBeforeAsync(ProtocolEvent.CurrentVersion, cancellationToken))
            total = Plus(total, await deletion.DeleteAsync(id, cancellationToken));

        if (total.Any) Logged(total);
        return total;
    }

    /// <summary>Two deletions' counts added together.</summary>
    private static DeletedRun Plus(DeletedRun a, DeletedRun b) =>
        new(a.Runs + b.Runs, a.Conversations + b.Conversations, a.Turns + b.Turns, a.Requests + b.Requests,
            a.Notes + b.Notes, a.Grades + b.Grades, a.Calibrations + b.Calibrations);

    /// <summary>The one line that says what went.</summary>
    private void Logged(DeletedRun total) =>
        log.Log(LogLevel.Information, ApiLogMessage.EvalOldRunsDeleted,
            new
            {
                total.Runs,
                Version = ProtocolEvent.CurrentVersion,
                total.Conversations,
                total.Turns,
                total.Requests,
                total.Notes,
                total.Grades,
                total.Calibrations
            }, tag: "API");
}

namespace LouisAgent.Orchestrator.Triage;

/// <summary>
/// Tells Ctrl+C apart from a request that was cancelled for another reason, so only your stop ends the run.
/// </summary>
internal static class Stopping
{
    /// <summary>
    /// Only the run's own stop token counts. An HTTP timeout is an <see cref="OperationCanceledException"/> too
    /// (<see cref="TaskCanceledException"/>), but that is one error's failure, not a reason to end the run (FX7).
    /// </summary>
    public static bool ByUser(Exception ex, CancellationToken stop) =>
        ex is OperationCanceledException && stop.IsCancellationRequested;
}

namespace LouisAgent.Api.Evals;

/// <summary>
/// The two signals a run's work is handed: the one it plays under, which the host's shutdown and a person's stop both
/// cancel, and the person's own, so the work can tell a stop, which it keeps as stopped, from a shutdown, which it
/// leaves as it was.
/// </summary>
/// <param name="Playing">The token the run plays under: cancelled by a shutdown or a stop.</param>
/// <param name="Stopped">Cancelled only when a person stopped the run.</param>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads">Cancellation in managed threads</seealso>
public sealed record RunSignals(CancellationToken Playing, CancellationToken Stopped);

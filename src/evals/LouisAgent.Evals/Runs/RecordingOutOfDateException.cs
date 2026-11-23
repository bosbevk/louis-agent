namespace LouisAgent.Evals.Runs;

/// <summary>
/// A replay asked its recording something the recording doesn't hold: a prompt, a doc the agent reads or a
/// criterion's words changed since the live run. It is the harness failing, not the agent, so it stops the run.
/// </summary>
/// <param name="reason">What the miss says changed.</param>
public sealed class RecordingOutOfDateException(string reason) : Exception(reason);

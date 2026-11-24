namespace LouisAgent.Core.Agent.Replay;

/// <summary>
/// A replay asked a tool something its recording never saw, so the tool wasn't run. Its message names the tool and
/// the trial.
/// </summary>
/// <remarks>
/// Replace Error Code with Exception (Fowler), as <c>ModelReplayMissException</c> is for the model: a named miss tells
/// a test, and a reader of the log, that the recording is out of date rather than that the tool broke. The tool loop
/// hands the model its message, so in a free replay the next model request changes and the eval says a live run is
/// needed.
/// </remarks>
public sealed class ToolReplayMissException(string message) : InvalidOperationException(message);

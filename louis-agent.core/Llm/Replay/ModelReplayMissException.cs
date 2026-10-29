namespace LouisAgent.Core.Llm.Replay;

/// <summary>
/// A replay asked the model something it has no recorded answer for, so the model was not called. Its message names
/// the trial and what changed in the request, as the replay client words it.
/// </summary>
/// <remarks>
/// Replace Error Code with Exception (Fowler): a miss used to be a plain <see cref="InvalidOperationException"/>, so a
/// caller could only tell it from any other failure by its text. A named exception lets the API stream it as a replay
/// miss, and the eval runner mark the turn so (FX26, Louis's pick). It stays an <see cref="InvalidOperationException"/>
/// underneath, so code that catches that still does.
/// </remarks>
public sealed class ModelReplayMissException(string message) : InvalidOperationException(message);

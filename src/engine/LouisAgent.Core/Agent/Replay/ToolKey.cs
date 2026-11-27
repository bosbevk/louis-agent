namespace LouisAgent.Core.Agent.Replay;

/// <summary>
/// What a tool was asked, as the replay keys it: the tool's name, the model's id for the call and its arguments as the
/// model wrote them, in JSON. The same call in a replayed model answer makes the same key, and two calls with the same
/// arguments make two, so each is played back with its own answer (FX258). A call made outside the tool loop has no id.
/// </summary>
/// <remarks>
/// Louis's pick (FX258): the call's id, over counting each call per trial or matching by the recording's order. A
/// replay plays the model's recorded answers whole, so every call comes back with the id it had live: exact, with no
/// state to reset, across every session of a trial.
/// </remarks>
public sealed record ToolKey(string Tool, string? Call, string Arguments);

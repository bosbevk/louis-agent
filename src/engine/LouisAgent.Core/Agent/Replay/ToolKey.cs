namespace LouisAgent.Core.Agent.Replay;

/// <summary>
/// What a tool was asked, as the replay keys it: the tool's name and its arguments as the model wrote them, in JSON.
/// The same call in a replayed model answer makes the same key.
/// </summary>
public sealed record ToolKey(string Tool, string Arguments);

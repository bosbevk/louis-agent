namespace LouisAgent.Core.Sessions.SubSessions;

/// <summary>
/// One event of a sub-session's stream (F5): the asked agent's thinking, text, tool calls and results, and its usage,
/// as the peer streamed them (<paramref name="Type"/> and <paramref name="Data"/> are its SSE event name and JSON),
/// tagged with the <c>AskAgent</c> call that opened it. For showing the work; the model only gets the final answer.
/// </summary>
public sealed record SubSessionEvent(string? ToolUseId, string Agent, string Type, string Data);

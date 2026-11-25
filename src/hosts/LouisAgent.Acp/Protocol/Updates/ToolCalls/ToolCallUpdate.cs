namespace LouisAgent.Acp.Protocol.Updates.ToolCalls;

/// <summary>A tool call finishing: its card turns done or failed, and shows a preview of what came back.</summary>
/// <param name="ToolCallId">The call this finishes.</param>
/// <param name="Status">Done or failed.</param>
/// <param name="Content">What the card shows.</param>
internal sealed record ToolCallUpdate(string ToolCallId, ToolCallStatus Status, IReadOnlyList<ToolCardContent> Content)
    : SessionUpdate;

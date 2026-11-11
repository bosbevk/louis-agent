using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// The short line under an asked agent's answer that says what it did ("SendLead, ShowPlace"), built in code from its
/// tool calls, so the asker knows without reading the whole stream.
/// </summary>
internal static class DidLine
{
    /// <summary>What these calls did, in a few words: each tool once, in the order first called.</summary>
    public static string Of(IReadOnlyList<AgentWork.ToolCall> calls) =>
        calls.Count == 0
            ? MessageText.Format(SubSessionToolMessage.AgentDidNothing)
            : string.Join(", ", calls.GroupBy(call => call.Name, StringComparer.Ordinal).Select(Tool));

    /// <summary>One tool's part: its name, how often when more than once, and how many failed.</summary>
    /// <remarks>A switch on the shape (ADR 0051), one arm per case, read like a table.</remarks>
    private static string Tool(IGrouping<string, AgentWork.ToolCall> calls) =>
        (Count: calls.Count(), Failed: calls.Count(call => call.Failed)) switch
        {
            { Count: 1, Failed: 0 } => calls.Key,
            { Count: 1 } => MessageText.Format(SubSessionToolMessage.AgentDidFailed, new { Tool = calls.Key }),
            { Failed: 0 } tool => MessageText.Format(SubSessionToolMessage.AgentDidTimes,
                new
                {
                    Tool = calls.Key,
                    tool.Count
                }),
            var tool => MessageText.Format(SubSessionToolMessage.AgentDidTimesFailed,
                new
                {
                    Tool = calls.Key,
                    tool.Count,
                    tool.Failed
                })
        };
}

using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// The short line under an asked agent's answer that says what it did, built in code from its tool calls: each call in
/// order with the first line of what it gave back ("ShowPage: Shown: /rooms/garden; SendLead: Sent"), so the asker
/// retells it right without reading the whole stream.
/// </summary>
/// <remarks>
/// Each call's own result since F127.4.2 (Louis's pick 6, of three: its result's first line cut at 80 characters, not
/// the whole result, which floods the asker's context, nor only the failures): the asker needs what happened, and a
/// tool's first line says it.
/// </remarks>
internal static class DidLine
{
    /// <summary>The most of a result the line keeps, a mark of where it was cut included.</summary>
    public const int ResultLength = 80;

    /// <summary>What these calls did, each in the order made.</summary>
    public static string Of(IReadOnlyList<AgentWork.ToolCall> calls) =>
        calls.Count == 0
            ? MessageText.Format(SubSessionToolMessage.AgentDidNothing)
            : string.Join("; ", calls.Select(Call));

    /// <summary>One call's part: its name, and what came back when anything did.</summary>
    /// <remarks>A switch on the shape (ADR 0051), one arm per case, read like a table.</remarks>
    private static string Call(AgentWork.ToolCall call) =>
        (call.Failed, Result: FirstLine(call.Result)) switch
        {
            (false, "") => call.Name,
            (false, var result) => MessageText.Format(SubSessionToolMessage.AgentDidCall,
                new
                {
                    Tool = call.Name,
                    Result = result
                }),
            (true, var result) => MessageText.Format(SubSessionToolMessage.AgentDidCallFailed,
                new
                {
                    Tool = call.Name,
                    Result = result
                })
        };

    /// <summary>A result's first line, cut short with a mark when it runs over.</summary>
    private static string FirstLine(string result)
    {
        string line = result.Split('\n', 2)[0].Trim();
        return line.Length <= ResultLength ? line : line[..(ResultLength - 1)] + "…";
    }
}

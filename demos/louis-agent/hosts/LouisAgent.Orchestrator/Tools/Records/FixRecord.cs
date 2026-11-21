using LouisAgent.Composition.Tools;
using System.Text.Json.Serialization;
using LouisAgent.Orchestrator.Client;
using LouisAgent.Orchestrator.Comms;

namespace LouisAgent.Orchestrator.Tools.Records;

/// <summary>
/// One line of fixes.jsonl, and what CallLouisAgentFix hands the model: how louis-agent's fix of one error went, by its
/// own FIX-RESULT, how many tools it called, what it used, and the end of what it said.
/// </summary>
/// <remarks>
/// A named record instead of an anonymous object (ADR 0041), with the snake_case keys every orchestrator file uses.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record FixRecord(
    [property: JsonPropertyName("error_id")]
    string ErrorId,
    [property: JsonPropertyName("session_id")]
    string SessionId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("commit")] string? Commit,
    [property: JsonPropertyName("branch")] string? Branch,
    [property: JsonPropertyName("tests")] string? Tests,
    [property: JsonPropertyName("summary")]
    string? Summary,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("tool_calls")]
    int ToolCalls,
    [property: JsonPropertyName("usage")] UsageLine? Usage,
    [property: JsonPropertyName("louis_agent_said")]
    string LouisAgentSaid)
{
    /// <summary>How much of louis-agent's last reply the line keeps.</summary>
    private const int SaidChars = 1500;

    /// <summary>
    /// The line for one fix conversation. Without a FIX-RESULT the status is "no-result", and the reason is the
    /// reply's error if it had one.
    /// </summary>
    public static FixRecord Of(string errorId, FixConversation conversation, AgentReply reply, FixResult? result) =>
        new(errorId, conversation.SessionId, result?.Status ?? "no-result", result?.Commit, result?.Branch,
            result?.Tests, result?.Summary, result?.Reason ?? reply.Error, conversation.ToolCalls,
            UsageLine.Of(conversation.Usage, conversation.Currency), ProcessRunner.Tail(reply.Text, SaidChars));
}

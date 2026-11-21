using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Orchestrator.Client;

/// <summary>What louis-agent answered in one message: its text, the tools it called, and how the turn ended.</summary>
/// <param name="Usage">
/// What the message's model requests used and cost, from <c>message_delta.usage</c> (F3); null when louis-agent doesn't
/// record usage or the turn ended in an error.
/// </param>
/// <param name="Currency">The cost's currency; null when nothing was priced.</param>
internal sealed record AgentReply(
    string Text,
    IReadOnlyList<AgentToolCall> ToolCalls,
    string? StopReason,
    string? Error,
    UsageTotal? Usage = null,
    string? Currency = null);

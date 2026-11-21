using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Client;
using LouisAgent.Packs.Triage.Feed;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// What the comms log remembers about one error while it's being handled: the messages and tool calls of its fix,
/// louis-agent's last FIX-RESULT, the orchestrator's own checks, and in the end the decision and what it all used.
/// </summary>
internal sealed class EventSummary(int number, ServiceError error)
{
    public int Number { get; } = number;
    public ServiceError Error { get; } = error;
    public int Messages { get; set; }
    public int ToolCalls { get; private set; }
    public FixResult? Fix { get; private set; }
    public List<string> Checks { get; } = [];
    public TriageOutcome Outcome { get; set; } = TriageOutcome.Unknown;
    public string Reason { get; set; } = "";

    /// <summary>The usage louis-agent reported for each reply of this event's fix.</summary>
    public List<UsageTotal> LouisAgentReplies { get; } = [];

    public EventUsage Usage { get; set; } = EventUsage.None;

    /// <summary>Counts one of louis-agent's replies: its tool calls, its FIX-RESULT if any, and its usage.</summary>
    public void Took(AgentReply reply, FixResult? result)
    {
        ToolCalls += reply.ToolCalls.Count;
        Fix = result ?? Fix;
        if (reply.Usage is { } used) LouisAgentReplies.Add(used);
    }
}

using LouisAgent.Composition.Logging;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Orchestrator.Tools;

/// <summary>
/// One fix session on louis-agent, as the orchestrator talks to it: every message goes into the comms log both ways,
/// and the tool calls, usage and currency of every reply are added up for fixes.jsonl.
/// </summary>
/// <param name="louisAgent">The client for louis-agent's API.</param>
/// <param name="comms">The comms log the conversation is written to.</param>
/// <param name="sessionId">The fix session, opened by the caller and closed by it too.</param>
internal sealed class FixConversation(LouisAgentClient louisAgent, CommsLog comms, string sessionId)
{
    private static readonly ILogger Log = AgentLogging.For<FixConversation>();

    private readonly List<UsageTotal> _usage = [];

    /// <summary>The fix session on louis-agent.</summary>
    public string SessionId { get; } = sessionId;

    /// <summary>How many tools louis-agent called across every reply.</summary>
    public int ToolCalls { get; private set; }

    /// <summary>The currency louis-agent priced its first priced reply in.</summary>
    public string? Currency { get; private set; }

    /// <summary>What louis-agent reported for every reply (F3); nothing when it doesn't record usage.</summary>
    public UsageTotal? Usage => _usage.Count == 0 ? null : UsageReport.Sum("louis-agent", _usage);

    /// <summary>Sends one message and waits for the reply, logging both and counting what the reply used.</summary>
    public async Task<AgentReply> SendAsync(string message, CancellationToken cancellationToken)
    {
        comms.Sent(SessionId, message);
        var answer = await louisAgent.SendAsync(SessionId, message, Progress, cancellationToken);
        comms.Received(answer, LouisAgentClient.ParseFixResult(answer.Text));
        ToolCalls += answer.ToolCalls.Count;
        if (answer.Usage is { } used) _usage.Add(used);
        Currency ??= answer.Currency;
        return answer;
    }

    /// <summary>louis-agent's progress lines, passed on to the orchestrator's log as they come.</summary>
    private static void Progress(string line) =>
        Log.Log(LogLevel.Information, OrchestratorLogMessage.LouisAgentProgress, new { Line = line },
            tag: "louis-agent");
}

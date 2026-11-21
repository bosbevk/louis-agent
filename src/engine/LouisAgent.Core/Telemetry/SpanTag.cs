using LouisAgent.Choices;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// The tags a turn's span carries: the GenAI conventions' names where they have one, ours under <c>louis_agent.</c>
/// for the rest. Ids and numbers only, never a word of the conversation.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum SpanTag
{
    /// <summary>What the span is: <c>invoke_agent</c> for a turn.</summary>
    [WireName("gen_ai.operation.name")] OperationName,

    /// <summary>The agent that took the turn: its role, or the default mode.</summary>
    [WireName("gen_ai.agent.name")] AgentName,

    /// <summary>The session the turn is in.</summary>
    [WireName("gen_ai.conversation.id")] ConversationId,

    /// <summary>The host that ran it (<c>api</c>, <c>cli</c>, <c>orchestrator</c>…).</summary>
    [WireName("louis_agent.host")] Host,

    /// <summary>The turn's number in its session.</summary>
    [WireName("louis_agent.turn")] Turn,

    /// <summary>The run it belongs to, an eval's or the orchestrator's.</summary>
    [WireName("louis_agent.run")] Run,

    /// <summary>What the work is for, as whoever started it named it (a fix, an eval's conversation).</summary>
    [WireName("louis_agent.task")] Task,
}

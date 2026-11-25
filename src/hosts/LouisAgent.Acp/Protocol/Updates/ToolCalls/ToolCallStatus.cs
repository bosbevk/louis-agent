using System.Text.Json.Serialization;
using LouisAgent.Choices;

namespace LouisAgent.Acp.Protocol.Updates.ToolCalls;

/// <summary>Where a tool call stands, as Rider's card shows it: running, done, or failed.</summary>
/// <seealso href="https://agentclientprotocol.com/protocol/tool-calls">ACP: tool calls</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
[JsonConverter(typeof(JsonStringEnumConverter<ToolCallStatus>))]
internal enum ToolCallStatus
{
    [WireName("in_progress")] InProgress,

    [WireName("completed")] Completed,

    [WireName("failed")] Failed,
}

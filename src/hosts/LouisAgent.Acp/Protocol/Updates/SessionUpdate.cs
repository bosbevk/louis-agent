using System.Text.Json.Serialization;
using LouisAgent.Acp.Protocol.Updates.ToolCalls;

namespace LouisAgent.Acp.Protocol.Updates;

/// <summary>
/// One thing that happened in a chat, as ACP tells Rider about it: a piece of the answer, a piece of thinking, a tool
/// call starting, or one finishing. The kind travels in the update's <c>sessionUpdate</c> field.
/// </summary>
/// <remarks>
/// A discriminated union, with System.Text.Json's own polymorphism: each kind we send is a sealed record of its own,
/// declared here with its wire name, and the serializer writes that name first. Chosen over the unofficial
/// AgentClientProtocol package and over flat records that each carry their kind as a string, so the kinds are one
/// declared list and the JSON Rider reads stays as it was.
/// </remarks>
/// <seealso href="https://agentclientprotocol.com/protocol/prompt-turn">ACP: the prompt turn and its updates</seealso>
/// <seealso href="https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/polymorphism">System.Text.Json: polymorphism</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "sessionUpdate")]
[JsonDerivedType(typeof(AgentThoughtChunk), "agent_thought_chunk")]
[JsonDerivedType(typeof(AgentMessageChunk), "agent_message_chunk")]
[JsonDerivedType(typeof(ToolCall), "tool_call")]
[JsonDerivedType(typeof(ToolCallUpdate), "tool_call_update")]
internal abstract record SessionUpdate;

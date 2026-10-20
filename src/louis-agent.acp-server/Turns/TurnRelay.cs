namespace LouisAgent.AcpServer.Turns;

using LouisAgent.Core.Agent.Turns;
using LouisAgent.AcpServer.Protocol;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;

/// <summary>
/// Relays a turn to the client as it happens (S, CL15, from AcpServer): the model's thinking (agent_thought_chunk),
/// text as it is written, and each tool call as a tool_call that is updated to completed/failed when its result arrives.
/// </summary>
internal sealed class TurnRelay(AgentEngine agent, JsonRpcWriter writer)
{
    public async Task StreamAsync(string sessionId, AcpSession session, string prompt,
        CancellationToken turnCancellationToken, CancellationToken serverCancellationToken)
    {
        // A new message id after each tool call, so text before and after a tool shows as separate messages.
        string messageId = $"msg_{Guid.NewGuid():N}";
        await foreach (var update in agent.StreamPromptAsync(session.History, prompt, turnCancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent { Text.Length: > 0 } thinking:
                        await writer.WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "agent_thought_chunk",
                                content = new { type = "text", text = thinking.Text }
                            }
                        }, serverCancellationToken);
                        break;
                    case TextContent { Text.Length: > 0 } text:
                        await MessageChunkAsync(sessionId, messageId, text.Text, serverCancellationToken);
                        break;
                    case FunctionCallContent call:
                        messageId = $"msg_{Guid.NewGuid():N}";
                        await writer.WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "tool_call",
                                toolCallId = call.CallId,
                                title = $"{call.Name} {ToolCallText.Describe(call.Arguments)}".Trim(),
                                kind = ToolCards.Kind(call.Name),
                                status = "in_progress",
                                rawInput = call.Arguments,
                            }
                        }, serverCancellationToken);
                        break;
                    case FunctionResultContent result:
                        string output = result.Result?.ToString() ?? result.Exception?.Message ?? string.Empty;
                        bool failed = result.Exception is not null || output.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
                        await writer.WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "tool_call_update",
                                toolCallId = result.CallId,
                                status = failed ? "failed" : "completed",
                                content = new[]
                                {
                                    new
                                    {
                                        type = "content",
                                        content = new { type = "text", text = ToolCards.Preview(output) }
                                    }
                                },
                            }
                        }, serverCancellationToken);
                        break;
                }
            }
        }
    }

    /// <summary>One piece of the agent's message, under <paramref name="messageId"/>.</summary>
    public Task MessageChunkAsync(string sessionId, string messageId, string text, CancellationToken cancellationToken) =>
        writer.WriteNotificationAsync("session/update", new
        {
            sessionId,
            update = new
            {
                sessionUpdate = "agent_message_chunk",
                messageId,
                content = new { type = "text", text }
            }
        }, cancellationToken);
}

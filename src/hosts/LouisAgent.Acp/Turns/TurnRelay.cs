using LouisAgent.Acp.Protocol;
using LouisAgent.Acp.Protocol.Updates;
using LouisAgent.Acp.Protocol.Updates.ToolCalls;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns;
using Microsoft.Extensions.AI;

namespace LouisAgent.Acp.Turns;

/// <summary>
/// Tells Rider about a turn while it happens (S, CL15, from AcpServer): the model's thinking, its answer as it is
/// written, and each tool call as a card that turns done or failed when its result comes back.
/// </summary>
internal sealed class TurnRelay(AgentEngine agent, JsonRpcWriter writer)
{
    /// <summary>Runs the prompt on the session's chat and sends Rider an update for each thing that happens.</summary>
    public async Task StreamAsync(string sessionId, AcpSession session, string prompt,
        CancellationToken turnCancellationToken, CancellationToken serverCancellationToken)
    {
        // A new message id after each tool call, so text before and after a tool shows as separate messages.
        string messageId = $"msg_{Guid.NewGuid():N}";
        // Every piece of every update, in the order they came, as one stream.
        var contents = agent.StreamPromptAsync(session.Conversation, prompt, turnCancellationToken)
            .SelectMany(update => update.Contents);
        await foreach (var content in contents)
        {
            switch (content)
            {
                case TextReasoningContent { Text.Length: > 0 } thinking:
                    var thought = new AgentThoughtChunk(new TextBlock(thinking.Text));
                    await SendAsync(sessionId, thought, serverCancellationToken);
                    break;
                case TextContent { Text.Length: > 0 } text:
                    await MessageChunkAsync(sessionId, messageId, text.Text, serverCancellationToken);
                    break;
                case FunctionCallContent call:
                    messageId = $"msg_{Guid.NewGuid():N}";
                    await SendAsync(sessionId, Started(call), serverCancellationToken);
                    break;
                case FunctionResultContent result:
                    await SendAsync(sessionId, Finished(result), serverCancellationToken);
                    break;
            }
        }
    }

    /// <summary>One piece of the agent's message, under <paramref name="messageId"/>.</summary>
    public Task MessageChunkAsync(string sessionId, string messageId, string text,
        CancellationToken cancellationToken)
    {
        var chunk = new AgentMessageChunk(messageId, new TextBlock(text));
        return SendAsync(sessionId, chunk, cancellationToken);
    }

    /// <summary>The card a tool call opens: the tool and what it was asked, and its kind for the icon.</summary>
    private static ToolCall Started(FunctionCallContent call)
    {
        string title = $"{call.Name} {ToolCallText.Describe(call.Arguments)}".Trim();
        return new ToolCall(call.CallId, title, ToolCards.Kind(call.Name), ToolCallStatus.InProgress, call.Arguments);
    }

    /// <summary>
    /// The card's end: failed when the tool threw or its answer starts with "Error", done otherwise, with a preview of
    /// what came back.
    /// </summary>
    private static ToolCallUpdate Finished(FunctionResultContent result)
    {
        string output = result.Result?.ToString() ?? result.Exception?.Message ?? string.Empty;
        bool failed = result.Exception is not null ||
                      output.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
        var status = failed ? ToolCallStatus.Failed : ToolCallStatus.Completed;
        ToolCardContent preview = new(new TextBlock(ToolCards.Preview(output)));
        return new ToolCallUpdate(result.CallId, status, [preview]);
    }

    /// <summary>Sends one update for the session.</summary>
    private Task SendAsync(string sessionId, SessionUpdate update, CancellationToken cancellationToken)
    {
        var notification = new SessionUpdateNotification(sessionId, update);
        return writer.WriteNotificationAsync(SessionUpdateNotification.Method, notification, cancellationToken);
    }
}

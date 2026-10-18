using System.Text.Json;
using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Web.Shared;

/// <summary>Folds louis-agent.api's streamed events (Claude's shape) into a <see cref="Turn"/>, for every chat page.</summary>
public static class TurnStream
{
    public static void Apply(Turn turn, AgentStreamEvent streamEvent)
    {
        var data = streamEvent.Data;
        switch (streamEvent.Type)
        {
            case "message_start":
                // F34: what the session loaded; a specialist's role may add toolsets to the ones its parent sent.
                if (data.TryGetProperty("message", out var message) && message.TryGetProperty("toolsets", out var loaded)
                    && loaded.ValueKind == JsonValueKind.Array)
                    turn.Toolsets = [.. loaded.EnumerateArray().Select(t => t.GetString()).OfType<string>()];
                break;

            case "content_block_start":
                var block = data.GetProperty("content_block");
                switch (block.GetProperty("type").GetString())
                {
                    case "thinking":
                        turn.Blocks.Add(new Block { Kind = BlockKind.Thinking });
                        break;
                    case "text":
                        turn.Blocks.Add(new Block { Kind = BlockKind.Text });
                        break;
                    case "tool_use":
                        turn.Blocks.Add(new Block
                        {
                            Kind = BlockKind.Tool,
                            ToolUseId = block.GetProperty("id").GetString(),
                            ToolName = block.GetProperty("name").GetString(),
                            ToolInput = FormatInput(block.GetProperty("input")),
                        });
                        break;
                    case "tool_result":
                        string? toolUseId = block.GetProperty("tool_use_id").GetString();
                        if (turn.Blocks.LastOrDefault(b => b.ToolUseId == toolUseId) is { } tool)
                        {
                            tool.ToolResult = block.GetProperty("content").GetString() ?? "";
                            tool.ToolFailed = block.GetProperty("is_error").GetBoolean();
                        }
                        break;
                }
                break;

            case "content_block_delta":
                var delta = data.GetProperty("delta");
                if (turn.Blocks.LastOrDefault() is not { } current) break;
                if (delta.TryGetProperty("thinking", out var thinking)) current.Text += thinking.GetString();
                else if (delta.TryGetProperty("text", out var text)) current.Text += text.GetString();
                break;

            case "content_block_stop":
                if (turn.Blocks.LastOrDefault() is { Kind: BlockKind.Thinking, ThinkingSeconds: null } finished)
                    finished.ThinkingSeconds = (DateTimeOffset.Now - finished.StartedAt).TotalSeconds;
                break;

            case "message_delta":
                turn.StopReason = data.GetProperty("delta").GetProperty("stop_reason").GetString();
                // Only on turns that called the model, and only from an API that records usage.
                if (data.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                    turn.Usage = ReadUsage(usage);
                break;

            case "sub_session":
                // F5: an event of a sub-session a StartSubSession call opened, folded into that call's own turn the same way.
                // F30: a minion's events arrive inside the specialist's as a nested sub_session, so this recurses a level.
                string? callId = data.TryGetProperty("tool_use_id", out var id) ? id.GetString() : null;
                var call = (callId is null ? null : turn.Blocks.LastOrDefault(b => b.ToolUseId == callId))
                           ?? turn.Blocks.LastOrDefault(b => Delegation.IsDelegationTool(b.ToolName));
                if (call is null || !data.TryGetProperty("event", out var inner) || inner.GetString() is not { } innerType) break;
                call.SubTurn ??= new Turn();
                if (data.TryGetProperty("agent", out var peer)) call.SubAgent ??= peer.GetString();
                Apply(call.SubTurn, new AgentStreamEvent(innerType, data.GetProperty("data")));
                break;

            case "error":
                turn.Error = data.GetProperty("error").GetProperty("message").GetString();
                // FX6: a failed turn still reports what its requests used, so the chat total matches the server's.
                if (data.TryGetProperty("usage", out var used) && used.ValueKind == JsonValueKind.Object)
                    turn.Usage = ReadUsage(used);
                break;
        }
    }

    private static TurnUsage ReadUsage(JsonElement usage)
    {
        long Count(string name) => usage.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;
        return new TurnUsage(
            Count("input_tokens"), Count("cache_creation_input_tokens"), Count("cache_read_input_tokens"), Count("output_tokens"),
            (int)Count("requests"),
            usage.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Number ? cost.GetDecimal() : null,
            usage.TryGetProperty("currency", out var currency) && currency.ValueKind == JsonValueKind.String ? currency.GetString() : null,
            (int)Count("unpriced"));
    }

    private static string? FormatInput(JsonElement input) =>
        input.ValueKind == JsonValueKind.Object && !input.EnumerateObject().Any()
            ? null
            : JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
}

using System.Text.Json;

namespace LouisAgent.Web.Shared.Chats;

using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;

/// <summary>Folds louis-agent.api's streamed events (Claude's shape, read by the session-stream reader) into a
/// <see cref="Turn"/>, for every chat page.</summary>
public static class TurnStream
{
    public static void Apply(Turn turn, SessionEvent streamEvent)
    {
        switch (streamEvent)
        {
            case MessageStarted started:
                // F34: what the session loaded; a specialist's role may add toolsets to the ones its parent sent.
                if (started.Toolsets.Count > 0) turn.Toolsets = [.. started.Toolsets];
                // FX17: its role, so a follow-up's card (whose call names only the handle) knows who it went to.
                if (started.Role is not null) turn.Role = started.Role;
                break;

            case ContentBlockStarted start:
                StartBlock(turn, start.Data.GetProperty("content_block"));
                break;

            case ContentBlockDelta delta:
                var change = delta.Data.GetProperty("delta");
                if (turn.Blocks.LastOrDefault() is not { } current) break;
                if (change.TryGetProperty("thinking", out var thinking)) current.Text += thinking.GetString();
                else if (change.TryGetProperty("text", out var text)) current.Text += text.GetString();
                break;

            case ContentBlockStopped:
                if (turn.Blocks.LastOrDefault() is { Kind: BlockKind.Thinking, ThinkingSeconds: null } finished)
                    finished.ThinkingSeconds = (DateTimeOffset.Now - finished.StartedAt).TotalSeconds;
                break;

            case MessageDelta messageDelta:
                turn.StopReason = messageDelta.Data.GetProperty("delta").GetProperty("stop_reason").GetString();
                // Only on turns that called the model, and only from an API that records usage.
                if (messageDelta.Data.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                    turn.Usage = ReadUsage(usage);
                break;

            case SubSessionStreamed sub:
                ApplySubSession(turn, sub);
                break;

            case ErrorReported error:
                turn.Error = error.Data.GetProperty("error").GetProperty("message").GetString();
                // FX6: a failed turn still reports what its requests used, so the chat total matches the server's.
                if (error.Data.TryGetProperty("usage", out var used) && used.ValueKind == JsonValueKind.Object)
                    turn.Usage = ReadUsage(used);
                break;
        }
    }

    private static void StartBlock(Turn turn, JsonElement block)
    {
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
    }

    /// <summary>F5: an event of a sub-session a StartSubSession call opened, folded into that call's own turn the same way.
    /// F30: a minion's events arrive inside the specialist's as a nested sub_session, so this recurses a level.</summary>
    private static void ApplySubSession(Turn turn, SubSessionStreamed sub)
    {
        string? callId = sub.ToolUseId.Length > 0 ? sub.ToolUseId : null;
        var call = (callId is null ? null : turn.Blocks.LastOrDefault(b => b.ToolUseId == callId))
                   ?? turn.Blocks.LastOrDefault(b => Delegation.IsDelegationTool(b.ToolName));
        // No call to fold into, or an event with no name (its "event" is missing): there is nothing to show, so it is
        // skipped. The reader gives such an event an empty EventType (SessionEventMapper.MapSubSession).
        if (call is null || sub.Inner is UnknownEvent { EventType: "" }) return;
        call.SubTurn ??= new Turn();
        if (sub.Data.TryGetProperty("agent", out var peer)) call.SubAgent ??= peer.GetString();
        Apply(call.SubTurn, sub.Inner);
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

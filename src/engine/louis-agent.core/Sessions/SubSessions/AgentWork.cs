using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.Core.Sessions.SubSessions;

/// <summary>
/// What an asked agent did while it answered: the events it streamed, as they came, kept with the conversation so the
/// asker can say what it did and read it later.
/// </summary>
/// <remarks>
/// A Value Object (Fowler): it never changes, so a follow-up's work is a new one (<see cref="Then"/>) that the chat's
/// registry swaps in under its lock (<see cref="SubSessionRegistry.Update"/>), and nothing else has to lock.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record AgentWork(IReadOnlyList<SubSessionEvent> Events)
{
    /// <summary>A conversation that has done nothing yet.</summary>
    public static readonly AgentWork None = new([]);

    /// <summary>This work with a later reply's events after it.</summary>
    public AgentWork Then(IReadOnlyList<SubSessionEvent> later) => new([.. Events, .. later]);

    /// <summary>
    /// The tools it called, in order, each marked when it failed; its thinking and its text are not calls.
    /// </summary>
    public IReadOnlyList<ToolCall> Calls()
    {
        var calls = new List<(string Id, string Name)>();
        var failed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in Blocks())
        {
            switch (Text(block, "type"))
            {
                case "tool_use":
                    calls.Add((Text(block, "id"), Text(block, "name")));
                    break;
                // A result marks its own call by id, whenever it arrives.
                case "tool_result" when block.TryGetProperty("is_error", out var error) &&
                                        error.ValueKind == JsonValueKind.True:
                    failed.Add(Text(block, "tool_use_id"));
                    break;
            }
        }

        return [.. calls.Select(call => new ToolCall(call.Name, failed.Contains(call.Id)))];
    }

    /// <summary>
    /// The blocks the agent itself started. A deeper child's arrive wrapped as its own kind of event, so they are left
    /// out.
    /// </summary>
    private IEnumerable<JsonElement> Blocks()
    {
        foreach (var streamed in Events)
        {
            using var document = JsonDocument.Parse(streamed.Data);
            if (SessionEvent.From(streamed.Type, document.RootElement) is not ContentBlockStarted started) continue;
            if (!started.Data.TryGetProperty("content_block", out var block)) continue;
            // A copy, since the document goes when the loop moves on.
            yield return block.Clone();
        }
    }

    /// <summary>A text field of a block, or nothing.</summary>
    private static string Text(JsonElement block, string name) =>
        block.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>One tool the agent called, and whether it failed.</summary>
    public sealed record ToolCall(string Name, bool Failed);
}

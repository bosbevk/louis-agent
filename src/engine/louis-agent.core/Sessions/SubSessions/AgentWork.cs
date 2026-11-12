using System.Text;
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
    /// The tools it called, in order, each with what came back and marked when it failed; its thinking and its text
    /// are not calls.
    /// </summary>
    public IReadOnlyList<ToolCall> Calls()
    {
        var calls = new List<(string Id, string Name)>();
        var results = new Dictionary<string, (string Content, bool Failed)>(StringComparer.Ordinal);
        foreach (var block in Blocks())
        {
            switch (Field(block, "type"))
            {
                case "tool_use":
                    calls.Add((Field(block, "id"), Field(block, "name")));
                    break;
                // A result belongs to its own call by id, whenever it arrives.
                case "tool_result":
                    results[Field(block, "tool_use_id")] = (Field(block, "content"),
                        block.TryGetProperty("is_error", out var error) && error.ValueKind == JsonValueKind.True);
                    break;
            }
        }

        return
        [
            .. calls.Select(call => results.TryGetValue(call.Id, out var result)
                ? new ToolCall(call.Name, result.Failed, result.Content)
                : new ToolCall(call.Name, false))
        ];
    }

    /// <summary>The blocks the agent itself started.</summary>
    private IEnumerable<JsonElement> Blocks() =>
        Typed().OfType<ContentBlockStarted>()
            .Where(started => started.Data.TryGetProperty("content_block", out _))
            .Select(started => started.Data.GetProperty("content_block"));

    /// <summary>
    /// The work step by step, as a person would retell it: each answer it began, what it said, each tool it called with
    /// what it sent, and what came back. Its thinking is left out.
    /// </summary>
    public IReadOnlyList<Step> Steps()
    {
        var steps = new List<Step>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var said = new StringBuilder();
        foreach (var typed in Typed())
        {
            switch (typed)
            {
                // What it says streams in pieces; they are one step until something else starts.
                case ContentBlockDelta delta when SaidPiece(delta.Data) is { } piece:
                    said.Append(piece);
                    break;
                case MessageStarted:
                    Flush(steps, said);
                    steps.Add(new Step(StepKind.Began, ""));
                    break;
                case ContentBlockStarted started when started.Data.TryGetProperty("content_block", out var block):
                    Flush(steps, said);
                    if (Retold(block, names) is { } step) steps.Add(step);
                    break;
            }
        }

        Flush(steps, said);
        return steps;
    }

    /// <summary>A call or a result as a step, the result named by its call; any other block is no step.</summary>
    private static Step? Retold(JsonElement block, Dictionary<string, string> names)
    {
        switch (Field(block, "type"))
        {
            case "tool_use":
                names[Field(block, "id")] = Field(block, "name");
                return new Step(StepKind.Called, block.TryGetProperty("input", out var input) ? input.GetRawText() : "",
                    Field(block, "name"));
            case "tool_result":
                return new Step(StepKind.Returned, Field(block, "content"),
                    names.GetValueOrDefault(Field(block, "tool_use_id")),
                    block.TryGetProperty("is_error", out var error) && error.ValueKind == JsonValueKind.True);
            default:
                return null;
        }
    }

    /// <summary>Ends what it was saying as one step, if it said anything.</summary>
    private static void Flush(List<Step> steps, StringBuilder said)
    {
        if (said.Length == 0) return;
        steps.Add(new Step(StepKind.Said, said.ToString()));
        said.Clear();
    }

    /// <summary>A piece of what it said, or null for a piece of its thinking or of a tool's input.</summary>
    private static string? SaidPiece(JsonElement data) =>
        data.TryGetProperty("delta", out var delta) && Field(delta, "type") == "text_delta"
            ? Field(delta, "text")
            : null;

    /// <summary>
    /// Every event read into its type. A deeper child's arrive wrapped as its own kind, so none of its blocks look like
    /// the agent's own.
    /// </summary>
    private IEnumerable<SessionEvent> Typed()
    {
        foreach (var streamed in Events)
        {
            using var document = JsonDocument.Parse(streamed.Data);
            // A copy, since the document goes when the loop moves on.
            yield return SessionEvent.From(streamed.Type, document.RootElement.Clone());
        }
    }

    /// <summary>A text field of a block, or nothing.</summary>
    private static string Field(JsonElement block, string name) =>
        block.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>One tool the agent called, whether it failed, and what came back (empty until it does).</summary>
    public sealed record ToolCall(string Name, bool Failed, string Result = "");

    /// <summary>
    /// One step of the work: what kind, its text (what it said, what it sent a tool, what came back), the tool's name
    /// for a call or a result, and whether a result was an error.
    /// </summary>
    public sealed record Step(StepKind Kind, string Text, string? Tool = null, bool Failed = false);

    /// <summary>What a step is.</summary>
    public enum StepKind
    {
        /// <summary>It began another answer: the first, or a follow-up's.</summary>
        Began,

        /// <summary>It said something to the asker.</summary>
        Said,

        /// <summary>It called a tool.</summary>
        Called,

        /// <summary>A tool gave back its result.</summary>
        Returned
    }
}

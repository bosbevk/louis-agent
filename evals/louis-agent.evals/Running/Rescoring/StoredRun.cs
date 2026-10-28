using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Running.Rescoring;

/// <summary>
/// One line of a stored run: the text as it was written, so a re-score can copy it untouched, and the event it holds.
/// </summary>
internal sealed record StoredLine(string Raw, EvalEvent Event);

/// <summary>
/// A stored run's protocol lines, read for a re-score or a label sheet: the split and the models its first line
/// names, and every event in order. The lines are only read, never changed.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed class StoredRun
{
    /// <summary>Keeps the lines, and reads the split and the models off the first line.</summary>
    private StoredRun(IReadOnlyList<StoredLine> lines, ProtocolEvent begin)
    {
        Lines = lines;
        Split = begin.Split;
        Models = begin.Models;
    }

    /// <summary>Every protocol line of the run, in the order it was written.</summary>
    public IReadOnlyList<StoredLine> Lines { get; }

    /// <summary>Which scenarios the run asked for.</summary>
    public RunSplit Split { get; }

    /// <summary>The model each part of the run ran on.</summary>
    public IReadOnlyList<ModelInfo> Models { get; }

    /// <summary>
    /// Reads a stored run's lines. A run with no first line can't be re-scored: a version 1 run is one, since its
    /// <c>@</c> lines are no events.
    /// </summary>
    public static StoredRun Read(IEnumerable<string> raw)
    {
        var lines = raw.Select(line => EvalProtocolJson.Read(line) is { } e ? new StoredLine(line, e) : null)
            .OfType<StoredLine>().ToList();
        var begin = lines.Select(line => line.Event).OfType<ProtocolEvent>().FirstOrDefault()
                    ?? throw new InvalidDataException(MessageText.Format(EvalErrorMessage.NoBeginLine));
        return new StoredRun(lines, begin);
    }

    /// <summary>The mode a <c>mode</c> line opened, as the graders see it.</summary>
    public static EvalMode ModeOf(ModeEvent mode) =>
        new(mode.Mode, mode.Role ?? string.Empty, mode.Toolsets, Llm: mode.Llm);

    /// <summary>
    /// Which conversation an event belongs to (its mode, repeat and task), for the events a conversation is made of.
    /// </summary>
    /// <remarks>A switch expression on the event's type, one arm per kind, read like a table.</remarks>
    public static ConversationKey? ConversationOf(EvalEvent e) => e switch
    {
        QuestionEvent q => q.Key.Conversation,
        SseEvent s => s.Key.Conversation,
        TurnEvent t => t.Key.Conversation,
        ConversationEvent c => c.Key,
        _ => null,
    };
}

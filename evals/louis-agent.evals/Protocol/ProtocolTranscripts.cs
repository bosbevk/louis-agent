using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// A conversation's transcript, read back from the protocol's own lines: each turn's events from its sse lines, and
/// its session, question and duration from its turn line; a turn written twice (a retried trial) reads as its last
/// attempt. The stream's error is the sse line's error event, as the API sent it; the turn line's own error field is
/// the same error, and is not read twice.
/// </summary>
public static class ProtocolTranscripts
{
    /// <summary>The turns of one mode, repeat and task, out of a log that holds many conversations.</summary>
    public static Transcript Read(IEnumerable<string> lines, string mode, int repeat, string task) =>
        Collect(lines, element => IsOf(element, mode, repeat, task));

    /// <summary>Every turn of a file that holds one conversation: no filter by mode, repeat or task.</summary>
    public static Transcript ReadConversation(IEnumerable<string> lines) => Collect(lines, _ => true);

    /// <summary>The mode of a conversation's lines: its first mode line, with its role and toolsets.</summary>
    public static EvalMode ReadMode(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            if (LineJson(line) is not { } element || element.GetProperty("event").GetString() != "mode") continue;

            var toolsets = element.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString() ?? string.Empty)
                .ToList();
            return new EvalMode(element.GetProperty("mode").GetString()!,
                element.GetProperty("role").GetString() ?? string.Empty, toolsets);
        }

        throw new InvalidDataException(MessageText.Format(EvalErrorMessage.NoModeLine));
    }

    /// <summary>Gathers each kept turn's stream events and its turn line into a transcript, in turn order.</summary>
    /// <remarks>A switch on the line's kind, one case per kind of turn line, read like a table.</remarks>
    private static Transcript Collect(IEnumerable<string> lines, Func<JsonElement, bool> keep)
    {
        var events = new Dictionary<int, List<SessionEvent>>();
        var sessions =
            new SortedDictionary<int, (string Session, string Question, double Seconds, TurnFailure? Failure)>();
        foreach (string line in lines)
        {
            if (LineJson(line) is not { } element) continue;
            string? kind = element.GetProperty("event").GetString();
            if (kind is not ("question" or "sse" or "turn") || !keep(element)) continue;

            int turn = element.GetProperty("turn").GetInt32();
            switch (kind)
            {
                // A turn's question opens it afresh: a trial run twice after a transport failure writes the same turn
                // twice, and the last attempt is the turn.
                case "question":
                    events.Remove(turn);
                    break;
                case "sse":
                    if (!events.TryGetValue(turn, out var list))
                    {
                        list = [];
                        events[turn] = list;
                    }

                    list.Add(SessionEvent.From(element.GetProperty("type").GetString()!,
                        element.GetProperty("data").Clone()));
                    break;
                default:
                    sessions[turn] = (element.GetProperty("session").GetString()!,
                        element.GetProperty("question").GetString()!,
                        element.GetProperty("seconds").GetDouble(),
                        Failure(element));
                    break;
            }
        }

        return new Transcript(sessions.Select(s => new TurnTranscript(s.Key, s.Value.Session, s.Value.Question,
            events.TryGetValue(s.Key, out var list) ? list : [], TimeSpan.FromSeconds(s.Value.Seconds),
            s.Value.Failure)).ToList());
    }

    /// <summary>
    /// A protocol line's JSON, whichever version wrote it: version 1's <c>@</c> line, or one of version 2's events.
    /// Anything else is a person's line, and is nothing here.
    /// </summary>
    /// <remarks>
    /// The bridge while both versions are read (step 7c.2c.0): the conformance references and the stored v1 runs are
    /// version 1, the runner's own output is version 2 once 7c.2c lands. Step 7c.2e reads the records instead, and
    /// this goes.
    /// </remarks>
    internal static JsonElement? LineJson(string line)
    {
        if (line.StartsWith('@')) return JsonDocument.Parse(line.AsMemory(1)).RootElement.Clone();
        return EvalProtocolJson.Read(line) is null ? null : JsonDocument.Parse(line).RootElement.Clone();
    }

    /// <summary>Whether a line is about this mode, repeat and task.</summary>
    private static bool IsOf(JsonElement element, string mode, int repeat, string task) =>
        element.TryGetProperty("mode", out var m) && m.GetString() == mode
                                                  && element.TryGetProperty("repeat", out var r) &&
                                                  r.GetInt32() == repeat
                                                  && element.TryGetProperty("task", out var t) && t.GetString() == task;

    /// <summary>
    /// The infrastructure failure a turn's line names, in v1's spelling too; none when it names none, or one this build
    /// doesn't know (a run written by a newer harness).
    /// </summary>
    private static TurnFailure? Failure(JsonElement element) =>
        element.TryGetProperty("failure", out var name) &&
        SettingChoice.TryParse(name.GetString(), out TurnFailure? failure, V1Words.FailureAliases)
            ? failure
            : null;
}

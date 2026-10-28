using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// Turns the protocol's lines back into a conversation the graders can read: each turn's stream events, and its
/// session, question, time and failure from its turn line. If a turn was run twice (a retried trial), you get the last
/// go. Only the contract's events count; anything else, a version 1 <c>@</c> line too, is a line for people.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ProtocolTranscripts
{
    /// <summary>One conversation's turns, picked out of a log that holds many.</summary>
    public static Transcript Read(IEnumerable<string> lines, string mode, int repeat, string task) =>
        Of(Events(lines), key => key.Mode == mode && key.Repeat == repeat && key.Task == task);

    /// <summary>Every turn of a file that holds one conversation.</summary>
    public static Transcript ReadConversation(IEnumerable<string> lines) => Of(Events(lines), _ => true);

    /// <summary>Every turn among events already picked out for one conversation.</summary>
    public static Transcript Of(IEnumerable<EvalEvent> events) => Of(events, _ => true);

    /// <summary>The mode a conversation ran in, from its first mode line.</summary>
    public static EvalMode ReadMode(IEnumerable<string> lines) =>
        Events(lines).OfType<ModeEvent>().Select(mode => new EvalMode(mode.Mode, mode.Role ?? string.Empty,
            mode.Toolsets)).FirstOrDefault()
        ?? throw new InvalidDataException(MessageText.Format(EvalErrorMessage.NoModeLine));

    /// <summary>The events among the lines, in order; a line that holds none is left out.</summary>
    private static IEnumerable<EvalEvent> Events(IEnumerable<string> lines) =>
        lines.Select(EvalProtocolJson.Read).OfType<EvalEvent>();

    /// <summary>Gathers each kept turn's stream events and its turn line into a transcript, in turn order.</summary>
    /// <remarks>
    /// A switch on the event's type, one case per event a turn is made of, read like a table: the contract's records
    /// carry their own fields, so nothing is looked up by name.
    /// </remarks>
    private static Transcript Of(IEnumerable<EvalEvent> events, Func<TurnKey, bool> keep)
    {
        var streams = new Dictionary<int, List<SessionEvent>>();
        var turns = new SortedDictionary<int, TurnEvent>();
        foreach (var e in events)
        {
            switch (e)
            {
                // A turn's question opens it afresh: a trial run twice after a transport failure writes the same turn
                // twice, and the last go is the turn.
                case QuestionEvent question when keep(question.Key):
                    streams.Remove(question.Turn);
                    break;
                case SseEvent sse when keep(sse.Key):
                    if (!streams.TryGetValue(sse.Turn, out var stream)) streams[sse.Turn] = stream = [];
                    stream.Add(SessionEvent.From(sse.Type, sse.Data));
                    break;
                case TurnEvent turn when keep(turn.Key):
                    turns[turn.Turn] = turn;
                    break;
            }
        }

        return new Transcript([
            .. turns.Values.Select(turn => new TurnTranscript(turn.Turn, turn.Session, turn.Question,
                streams.GetValueOrDefault(turn.Turn) ?? [], TimeSpan.FromSeconds(turn.Seconds), turn.Failure))
        ]);
    }
}

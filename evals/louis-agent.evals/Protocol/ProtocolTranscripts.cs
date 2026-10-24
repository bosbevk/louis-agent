using System.Text.Json;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// A conversation's transcript, read back from the protocol's own lines (F18 step 4.2b): the turns of one mode, repeat and task, each
/// turn's events from its sse lines, and its session, question and duration from its turn line. The stream's error is the sse line's
/// error event, as the API sent it; the turn line's own error field is the same error, and is not read twice.
/// </summary>
public static class ProtocolTranscripts
{
    public static Transcript Read(IEnumerable<string> lines, string mode, int repeat, string task)
    {
        var events = new Dictionary<int, List<SessionEvent>>();
        var sessions = new SortedDictionary<int, (string Session, string Question, double Seconds)>();
        foreach (string line in lines)
        {
            if (!line.StartsWith('@')) continue;
            using var document = JsonDocument.Parse(line[1..]);
            var element = document.RootElement;
            string? kind = element.GetProperty("event").GetString();
            if (kind is not ("sse" or "turn") || !IsOf(element, mode, repeat, task)) continue;

            int turn = element.GetProperty("turn").GetInt32();
            if (kind == "sse")
            {
                if (!events.TryGetValue(turn, out var list))
                {
                    list = [];
                    events[turn] = list;
                }
                list.Add(SessionEvent.From(element.GetProperty("type").GetString()!, element.GetProperty("data").Clone()));
            }
            else
            {
                sessions[turn] = (element.GetProperty("session").GetString()!, element.GetProperty("question").GetString()!,
                    element.GetProperty("seconds").GetDouble());
            }
        }

        return new Transcript(sessions.Select(s => new TurnTranscript(s.Key, s.Value.Session, s.Value.Question,
            events.TryGetValue(s.Key, out var list) ? list : [], TimeSpan.FromSeconds(s.Value.Seconds), null)).ToList());
    }

    private static bool IsOf(JsonElement element, string mode, int repeat, string task) =>
        element.TryGetProperty("mode", out var m) && m.GetString() == mode
        && element.TryGetProperty("repeat", out var r) && r.GetInt32() == repeat
        && element.TryGetProperty("task", out var t) && t.GetString() == task;
}

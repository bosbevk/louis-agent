using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Running.Rescoring;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Labels;

/// <summary>
/// Makes the files a person labels from a stored run (F18 step 6.3): one per conversation, with every turn's question
/// and answer (the last answer is the one to label) and no label yet. Only scenarios that set what the grader reads
/// get in: you can't label "a meal for four" where nobody asked for four. The sheet is a spread, not a dump: one
/// conversation of each scenario first, then a second of each, so ten labels aren't ten of one dish, and asking twice
/// gives the same sheet.
/// </summary>
public static class LabelSheet
{
    /// <summary>
    /// Writes up to <paramref name="count"/> new label files and hands back their names, in the order they were
    /// chosen. A conversation whose last turn failed or said nothing has nothing to label, and a file that's already
    /// there is never written over.
    /// </summary>
    public static IReadOnlyList<string> Write(IReadOnlyList<EvalScenario> scenarios, string grader,
        IReadOnlyCollection<string> keys, string runId, IEnumerable<string> storedLines,
        string folder, int count)
    {
        // The scenarios that set every key the grader reads.
        var stored = StoredRun.Read(storedLines);
        var eligible = scenarios
            .Where(s => keys.All(key =>
                s.Expected.ValueKind == JsonValueKind.Object && s.Expected.TryGetProperty(key, out _)))
            .Select(s => s.Id).ToHashSet();

        // Each conversation's turn events, and the conversations of the scenarios the grader can label.
        var turnEvents = new Dictionary<(string Mode, int Repeat, string Task), List<EvalEvent>>();
        var conversations = new List<ConversationEvent>();
        foreach (var e in stored.Lines.Select(line => line.Event))
        {
            if (e is ConversationEvent conversation)
            {
                if (eligible.Contains(conversation.Task)) conversations.Add(conversation);
                continue;
            }

            if (StoredRun.ConversationOf(e) is not { } key) continue;
            if (!turnEvents.TryGetValue(key, out var events)) turnEvents[key] = events = [];
            events.Add(e);
        }

        var files = new List<LabelFile>();
        foreach (var c in conversations)
        {
            var turns = ProtocolTranscripts.Of(turnEvents.GetValueOrDefault((c.Mode, c.Repeat, c.Task)) ?? []).Turns;
            if (turns.Count == 0 || turns[^1].Failure is not null) continue;
            string answer = EngineEvents.AnswerOf(turns[^1]);
            // Every turn, question and answer, so the judge sees what a follow-up relies on (F18 step 6a.4f).
            if (answer.Length > 0)
                files.Add(new LabelFile(grader, c.Task, c.Mode, runId, c.Repeat,
                    [.. turns.Select(t => new LabelTurn(t.Question, EngineEvents.AnswerOf(t)))], null));
        }

        // A label a person gave (or a sheet already made) is never written over: the next conversation is taken
        // instead, so a second sheet for the same run is all new files.
        var chosen = Spread(files.Where(file => !File.Exists(Path.Combine(folder, NameOf(file)))), count);
        Directory.CreateDirectory(folder);
        foreach (var file in chosen)
            File.WriteAllText(Path.Combine(folder, NameOf(file)),
                JsonSerializer.Serialize(file, LabelJson.Options) + "\n");
        return [.. chosen.Select(NameOf)];
    }

    /// <summary>
    /// The file's name. The grader is in it, so two graders labelling the same answer get a file each, and the
    /// never-overwrite rule never skips one grader for another's.
    /// </summary>
    private static string NameOf(LabelFile file) =>
        $"{file.Grader}.{file.Scenario}.{file.Mode}.{file.Run}.{file.Repeat}.json";

    /// <summary>
    /// The first of each scenario, then the second of each, and so on, up to the count: scenarios in name order, and a
    /// scenario's conversations by repeat and mode.
    /// </summary>
    private static List<LabelFile> Spread(IEnumerable<LabelFile> files, int count)
    {
        var byScenario = files.GroupBy(f => f.Scenario).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.OrderBy(f => f.Repeat).ThenBy(f => f.Mode, StringComparer.Ordinal).ToList()).ToList();
        var chosen = new List<LabelFile>();
        for (int round = 0; chosen.Count < count && byScenario.Any(list => list.Count > round); round++)
            foreach (var list in byScenario)
                if (list.Count > round && chosen.Count < count)
                    chosen.Add(list[round]);
        return chosen;
    }
}

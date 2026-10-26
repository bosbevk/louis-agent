using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running.Rescoring;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Labels;

/// <summary>
/// The files a person labels (F18 step 6.3), made from a stored run's protocol export, the lines <c>--rescore</c> reads: one <see cref="LabelFile"/> per conversation, holding every
/// turn's question and answer (the last answer is the one to label) and no label yet. Only a scenario that sets what the grader reads (<paramref name="keys"/>) is in the sheet: "a meal for four" can't be labelled where no
/// four is asked. The sheet is a spread, not a dump: one conversation of each scenario first, then a second of each, and so on up to the count, in a fixed order, so ten labels are not ten
/// repeats of one dish and asking twice gives the same sheet. A conversation whose last turn failed or answered nothing has nothing to label, and a file that already exists is left as it is.
/// </summary>
public static class LabelSheet
{
    /// <returns>The names of the files written, in the order they were chosen.</returns>
    public static IReadOnlyList<string> Write(IReadOnlyList<EvalScenario> scenarios, string grader, IReadOnlyCollection<string> keys, string runId, IEnumerable<string> storedLines,
        string folder, int count)
    {
        var stored = StoredRun.Read(storedLines);
        var eligible = scenarios.Where(s => keys.All(key => s.Expected.ValueKind == JsonValueKind.Object && s.Expected.TryGetProperty(key, out _))).Select(s => s.Id).ToHashSet();

        var turnLines = new Dictionary<(string Mode, int Repeat, string Task), List<string>>();
        var conversations = new List<StoredLine>();
        foreach (var line in stored.Lines)
        {
            if (line.Kind is "question" or "sse" or "turn")
            {
                if (!turnLines.TryGetValue(line.Key, out var lines)) turnLines[line.Key] = lines = [];
                lines.Add(line.Raw);
            }
            else if (line.Kind == "conversation" && eligible.Contains(line.Task)) conversations.Add(line);
        }

        var files = new List<LabelFile>();
        foreach (var line in conversations)
        {
            var turns = ProtocolTranscripts.Read(turnLines.GetValueOrDefault(line.Key) ?? [], line.Mode, line.Repeat, line.Task).Turns;
            if (turns.Count == 0 || turns[^1].Failure is not null) continue;
            string answer = EngineEvents.AnswerOf(turns[^1]);
            // 6a.4f: every turn, question and answer, so the judge sees what a follow-up relies on when the labels are calibrated against.
            if (answer.Length > 0) files.Add(new LabelFile(grader, line.Task, line.Mode, runId, line.Repeat, [.. turns.Select(t => new LabelTurn(t.Question, EngineEvents.AnswerOf(t)))], null));
        }

        // A label a person has given (or a sheet already made) is never written over: the next conversation is taken instead, so a second sheet for the same run is all new files.
        var chosen = Spread(files.Where(file => !File.Exists(Path.Combine(folder, NameOf(file)))), count);
        Directory.CreateDirectory(folder);
        foreach (var file in chosen)
            File.WriteAllText(Path.Combine(folder, NameOf(file)), JsonSerializer.Serialize(file, LabelJson.Options) + "\n");
        return [.. chosen.Select(NameOf)];
    }

    /// <summary>The grader is in the name, so two graders labelling the same answer have two files, and the sheet's never-overwrite rule never skips one grader for another's.</summary>
    private static string NameOf(LabelFile file) => $"{file.Grader}.{file.Scenario}.{file.Mode}.{file.Run}.{file.Repeat}.json";

    /// <summary>The first of each scenario, then the second of each, and so on: scenarios in name order, a scenario's conversations by repeat and mode.</summary>
    private static List<LabelFile> Spread(IEnumerable<LabelFile> files, int count)
    {
        var byScenario = files.GroupBy(f => f.Scenario).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.OrderBy(f => f.Repeat).ThenBy(f => f.Mode, StringComparer.Ordinal).ToList()).ToList();
        var chosen = new List<LabelFile>();
        for (int round = 0; chosen.Count < count && byScenario.Any(list => list.Count > round); round++)
            foreach (var list in byScenario)
                if (list.Count > round && chosen.Count < count) chosen.Add(list[round]);
        return chosen;
    }
}

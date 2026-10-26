using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// People's grades as the label files the harness's loader reads (F18 step 6a.2): one file per conversation a person passed or failed, named
/// <c>{grader}.{scenario}.{mode}.{run}.{repeat}.json</c> as the harness names its own sheets, holding the grader, the conversation it labels (every turn's question and answer, so the
/// label outlives the run and a judge sees what a follow-up relies on, 6a.4f) and the person's pass and note. An unsure is never a label. The API doesn't reference the harness, so the record is its own, on the
/// same wire (snake_case, ADR 0041); <c>ExportedLabelFixtureTests</c> and the harness's <c>ExportedLabelLoadingTests</c> hold the two to one fixture until CL22 makes them one type.
/// </summary>
public static class ReviewLabels
{
    private static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string FileName(string grader, ConversationKey key, string run) => $"{grader}.{key.Task}.{key.Mode}.{run}.{key.Repeat}.json";

    /// <summary>The same wire on one line (6a.5b): what the runner reads on its stdin, one label per line.</summary>
    private static readonly JsonSerializerOptions LineWire = new(Wire) { WriteIndented = false };

    /// <summary>One label file's text for a decided grade and every turn of its conversation (6a.4f), in turn order; the last turn's answer is the one the grade is of.</summary>
    public static string Content(HumanGrade grade, IReadOnlyList<Turn> turns) => JsonSerializer.Serialize(Record(grade, turns), Wire) + "\n";

    /// <summary>The same label on one line, for the runner's stdin (6a.5b).</summary>
    public static string Line(HumanGrade grade, IReadOnlyList<Turn> turns) => JsonSerializer.Serialize(Record(grade, turns), LineWire);

    private static LabelRecord Record(HumanGrade grade, IReadOnlyList<Turn> turns) =>
        new(grade.Grader, grade.Conversation.Task, grade.Conversation.Mode, grade.Run, grade.Conversation.Repeat,
            [.. turns.OrderBy(t => t.Key.Turn).Select(t => new LabelTurnRecord(t.Question, t.Answer ?? ""))], new LabelBody(grade.Verdict == HumanVerdict.Pass, grade.Note));

    /// <summary>The zip of the files, one per decided grade with its conversation's turns.</summary>
    public static byte[] Zip(IReadOnlyList<(HumanGrade Grade, IReadOnlyList<Turn> Turns)> labels)
    {
        using var bytes = new MemoryStream();
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (grade, turns) in labels)
            {
                using var writer = new StreamWriter(zip.CreateEntry(FileName(grade.Grader, grade.Conversation, grade.Run)).Open(), new UTF8Encoding(false));
                writer.Write(Content(grade, turns));
            }
        return bytes.ToArray();
    }

    private sealed record LabelRecord(string Grader, string Scenario, string Mode, string Run, int Repeat, IReadOnlyList<LabelTurnRecord> Turns, LabelBody Label);

    private sealed record LabelTurnRecord(string Question, string Answer);

    private sealed record LabelBody([property: JsonPropertyName("pass")] bool Pass, string Note);
}

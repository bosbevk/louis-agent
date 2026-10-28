using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// Turns people's grades into the label files the harness's loader reads: one file per conversation a person passed
/// or failed, named <c>{grader}.{scenario}.{mode}.{run}.{repeat}.json</c> as the harness names its own sheets. An
/// unsure is never a label.
/// </summary>
/// <remarks>
/// A label holds the grader, every turn's question and answer, and the person's pass and note, so it outlives the run
/// and a judge sees what a follow-up question relies on. The API doesn't reference the harness, so the record is its
/// own, on the same snake_case wire; <c>ExportedLabelFixtureTests</c> and the harness's
/// <c>ExportedLabelLoadingTests</c> hold the two to one fixture until CL22 makes them one type.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ReviewLabels
{
    private static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>A label file's name, the way the harness names its own sheets.</summary>
    public static string FileName(string grader, ConversationKey key, string run) =>
        $"{grader}.{key.Task}.{key.Mode}.{run}.{key.Repeat}.json";

    /// <summary>The same wire on one line: what the runner reads on its stdin, one label per line.</summary>
    private static readonly JsonSerializerOptions LineWire = new(Wire) { WriteIndented = false };

    /// <summary>
    /// One label file's text for a decided grade and every turn of its conversation, in turn order. The grade is of
    /// the last turn's answer.
    /// </summary>
    public static string Content(HumanGrade grade, IReadOnlyList<Turn> turns) =>
        JsonSerializer.Serialize(Record(grade, turns), Wire) + "\n";

    /// <summary>The same label on one line, for the runner's stdin.</summary>
    public static string Line(HumanGrade grade, IReadOnlyList<Turn> turns) =>
        JsonSerializer.Serialize(Record(grade, turns), LineWire);

    /// <summary>The label as a record the serializer writes; a turn with no answer gets an empty one.</summary>
    private static LabelRecord Record(HumanGrade grade, IReadOnlyList<Turn> turns) =>
        new(grade.Grader, grade.Conversation.Task, grade.Conversation.Mode, grade.Run, grade.Conversation.Repeat,
            [.. turns.OrderBy(t => t.Key.Turn).Select(t => new LabelTurnRecord(t.Question, t.Answer ?? ""))],
            new LabelBody(grade.Verdict == HumanVerdict.Pass, grade.Note));

    /// <summary>A zip of the label files, one per decided grade with its conversation's turns.</summary>
    public static byte[] Zip(IReadOnlyList<(HumanGrade Grade, IReadOnlyList<Turn> Turns)> labels)
    {
        using var bytes = new MemoryStream();

        // The zip must be closed before its bytes are read, so it gets its own block and leaves the stream open.
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (grade, turns) in labels)
            {
                // Each file in UTF-8 without a byte-order mark, so the loader reads plain JSON.
                using var writer =
                    new StreamWriter(zip.CreateEntry(FileName(grade.Grader, grade.Conversation, grade.Run)).Open(),
                        new UTF8Encoding(false));
                writer.Write(Content(grade, turns));
            }

        return bytes.ToArray();
    }

    /// <summary>A label file as the harness reads it: which conversation, its turns, and the person's label.</summary>
    private sealed record LabelRecord(
        string Grader,
        string Scenario,
        string Mode,
        string Run,
        int Repeat,
        IReadOnlyList<LabelTurnRecord> Turns,
        LabelBody Label);

    /// <summary>One turn of the labelled conversation: what was asked and what came back.</summary>
    private sealed record LabelTurnRecord(string Question, string Answer);

    /// <summary>The person's label: pass or fail, and their note.</summary>
    private sealed record LabelBody([property: JsonPropertyName("pass")] bool Pass, string Note);
}

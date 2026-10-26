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
/// <c>{grader}.{scenario}.{mode}.{run}.{repeat}.json</c> as the harness names its own sheets, holding the grader, which answer it labels (the conversation's last turn and that turn's
/// answer text, so the label outlives the run) and the person's pass and note. An unsure is never a label. The API doesn't reference the harness, so the record is its own, on the
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

    /// <summary>One label file's text for a decided grade and the last turn of its conversation.</summary>
    public static string Content(HumanGrade grade, Turn lastTurn) =>
        JsonSerializer.Serialize(new LabelRecord(grade.Grader, grade.Conversation.Task, grade.Conversation.Mode, grade.Run, grade.Conversation.Repeat, lastTurn.Key.Turn,
            lastTurn.Answer ?? "", new LabelBody(grade.Verdict == HumanVerdict.Pass, grade.Note)), Wire) + "\n";

    /// <summary>The zip of the files, one per decided grade with its last turn.</summary>
    public static byte[] Zip(IReadOnlyList<(HumanGrade Grade, Turn LastTurn)> labels)
    {
        using var bytes = new MemoryStream();
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (grade, lastTurn) in labels)
            {
                using var writer = new StreamWriter(zip.CreateEntry(FileName(grade.Grader, grade.Conversation, grade.Run)).Open(), new UTF8Encoding(false));
                writer.Write(Content(grade, lastTurn));
            }
        return bytes.ToArray();
    }

    private sealed record LabelRecord(string Grader, string Scenario, string Mode, string Run, int Repeat, int Turn, string Answer, LabelBody Label);

    private sealed record LabelBody([property: JsonPropertyName("pass")] bool Pass, string Note);
}

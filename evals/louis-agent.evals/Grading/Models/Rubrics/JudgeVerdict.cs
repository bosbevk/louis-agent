using System.Text.Json;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// The judge's reply, as every rubric asks for it: <c>pass</c> true, false or null for "can't tell", and the reason
/// in a sentence.
/// </summary>
/// <remarks>
/// A record the serializer reads (ADR 0041). A model's reply isn't a file a person wrote, so the reading is not
/// strict: an extra field is left alone, while a reply with no verdict in it is no verdict, never a pass.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record JudgeVerdict(bool? Pass, string? Reason)
{
    /// <summary>
    /// The verdict in a reply that may hold words or a code fence around it: the last JSON object in the text, as a
    /// model that thinks aloud first may write braces before it. Null when there is none, or it isn't a verdict.
    /// </summary>
    public static JudgeVerdict? Read(string reply)
    {
        var end = reply.LastIndexOf('}');
        var start = end < 0 ? -1 : reply.LastIndexOf('{', end);
        if (start < 0) return null;
        try
        {
            return JsonSerializer.Deserialize<JudgeVerdict>(reply[start..(end + 1)], EvalJson.SnakeCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

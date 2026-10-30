using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Identities.Puls.Judges;

/// <summary>
/// What the judge answers: <c>meal</c> is yes, no, or null for "can't tell", with the reason in a sentence.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record MealVerdict(
    [property: JsonPropertyName("meal")] bool? Meal,
    [property: JsonPropertyName("reason")] string? Reason)
{
    /// <summary>Reads the names whatever their case, since a model may write <c>Meal</c>.</summary>
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Finds the verdict in a reply that may have words or a code fence around it: the last JSON object in the text,
    /// since a model that thinks aloud may write braces before it. You get nothing back if there is none, or if it
    /// isn't a verdict (<c>"meal": "yes"</c>).
    /// </summary>
    public static MealVerdict? Read(string reply)
    {
        int end = reply.LastIndexOf('}');
        int start = end < 0 ? -1 : reply.LastIndexOf('{', end);
        if (start < 0) return null;
        try
        {
            return JsonSerializer.Deserialize<MealVerdict>(reply[start..(end + 1)], Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

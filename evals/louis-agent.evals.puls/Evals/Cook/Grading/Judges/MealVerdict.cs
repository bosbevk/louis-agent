using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>The judge's reply (ADR 0041: a known shape is a record the serializer reads): <c>meal</c> true, false or null for "can't tell", and the reason in a sentence.</summary>
public sealed record MealVerdict([property: JsonPropertyName("meal")] bool? Meal, [property: JsonPropertyName("reason")] string? Reason)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The verdict in a reply that may hold words, or a code fence, around it: the last JSON object in the text (a model that thinks aloud first may write braces before it).
    /// Null when the reply holds none, or one that is not a verdict (<c>"meal": "yes"</c>).
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

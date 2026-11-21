using System.Text.Json;

namespace LouisAgent.Packs.Leads.Sending;

/// <summary>Resend's wire: snake_case names, nothing else changed. One options object for this one wire (ADR 0041).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ResendJson
{
    /// <summary>The options every request to Resend is written with.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

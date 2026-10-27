using System.Text.Json;

namespace LouisAgent.Api.Admin;

/// <summary>The one way the admin replies are written: the web defaults, with snake_case names.</summary>
/// <remarks>
/// One options object per wire (ADR 0041). The portal reads snake_case, so the rule lives here once and the reply
/// records stay plain C# names, where an attribute on every property would repeat it and could drift.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal static class AdminJson
{
    /// <summary>Pass this to <c>Results.Json</c> for an admin reply.</summary>
    public static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

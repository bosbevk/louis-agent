using System.Text.Json;

namespace LouisAgent.Api.Contracts.Info;

/// <summary>
/// The one way the instance's own replies (its health, model, roles, LLM profiles, toolsets and display currency) are
/// written and read: the web defaults, with snake_case names.
/// </summary>
/// <remarks>
/// One options object per wire (ADR 0041). These replies have always been snake_case; naming the rule here keeps the
/// records in plain C# names. A dictionary's keys (a currency code) keep their spelling.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class InfoJson
{
    /// <summary>Pass this to <c>TypedResults.Json</c> for an info reply, and to a client reading one.</summary>
    public static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

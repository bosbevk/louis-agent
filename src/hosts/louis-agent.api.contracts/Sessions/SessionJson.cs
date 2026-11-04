using System.Text.Json;

namespace LouisAgent.Api.Contracts.Sessions;

/// <summary>
/// The one way the sessions API's own replies (a session opened, read, cancelled or deleted) are written and read:
/// the web defaults, with snake_case names. The stream of a turn has its own format and options.
/// </summary>
/// <remarks>
/// One options object per wire (ADR 0041). The session replies have always been snake_case; naming the rule here keeps
/// the records in plain C# names, and the JSON every client reads stays as it was, byte for byte.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class SessionJson
{
    /// <summary>Pass this to <c>TypedResults.Json</c> for a session reply, and to a client reading one.</summary>
    public static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

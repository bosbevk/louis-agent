using System.Text.Json;

namespace LouisAgent.Api.Contracts.Admin;

/// <summary>The one way the admin replies are written and read: the web defaults, with snake_case names.</summary>
/// <remarks>
/// One options object per wire (ADR 0041). The portal reads snake_case, so the rule lives here once and the reply
/// records stay plain C# names, where an attribute on every property would repeat it and could drift. It sits in the
/// contract so the page reads with the same options the API wrote with.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public static class AdminJson
{
    /// <summary>
    /// Pass this to <c>TypedResults.Json</c> for an admin reply, and to <c>GetFromJsonAsync</c> to read one.
    /// </summary>
    public static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

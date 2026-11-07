using System.Text.Json;

namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// The one way the account routes' bodies and replies are written and read: the web defaults, camelCase, the same
/// Identity's own routes under <c>/account</c> use, so a client reads both with one setting.
/// </summary>
/// <remarks>One options object per wire (ADR 0041).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class AccountJson
{
    /// <summary>Pass this to <c>TypedResults.Json</c> for an account reply, and to a client reading or sending one.</summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

using System.Text.Json;

namespace LouisAgent.Api.Contracts.Workspace;

/// <summary>
/// The one way the code-review app's replies (the workspace and git) are written and read: the web defaults, with
/// camelCase names.
/// </summary>
/// <remarks>
/// One options object per wire (ADR 0041). This wire has always been ASP.NET's defaults; naming them here lets the API
/// pass them on purpose and the app read with the same ones.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class WorkspaceJson
{
    /// <summary>
    /// Pass this to <c>TypedResults.Json</c> for a workspace or git reply, and to a client reading one.
    /// </summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

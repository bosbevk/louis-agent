using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Runs;

/// <summary>
/// Shows what a run ran with: its eval and graders, the settings it was started with, its modes, and the lines the
/// runner printed. It was part of the run page, and moved out to keep that page under the size limit.
/// </summary>
/// <remarks>
/// Extract Component. The section only reads the run it is given and has nothing to do with the rest of the page, so
/// it can sit on its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
public partial class RunSettings
{
    /// <summary>The run as the API sent it.</summary>
    [Parameter, EditorRequired]
    public JsonElement Run { get; set; }

    /// <summary>
    /// The run's own row, read from the JSON for the grader list. It goes when this component takes the contract's
    /// reply (step 5, commit 3).
    /// </summary>
    private StoredRunRow RunRow => Run.GetProperty("run").Deserialize<StoredRunRow>(AdminJson.SnakeCase)!;

    /// <summary>A text from the JSON, or nothing if it is not there.</summary>
    private static string? S(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

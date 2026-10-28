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
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class RunSettings
{
    /// <summary>The run as the contract's stored run reply carries it.</summary>
    [Parameter, EditorRequired]
    public StoredRunReply Run { get; set; } = null!;

    /// <summary>A mode's toolsets, then its role and its model when it named them.</summary>
    private static string Extra(ModeRow mode) =>
        (mode.Role is { } role ? $" · {role}" : "") + (mode.Llm is { } llm ? $" · {llm}" : "");
}

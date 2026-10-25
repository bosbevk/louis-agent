using System.Text.Json;
using LouisAgent.Puls.Admin.Grades;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Grades;

/// <summary>The run's eval, version, commit and graders, read from the run's JSON (<see cref="RunGradersReader"/>).</summary>
public partial class GraderList
{
    /// <summary>The run as <c>GET /admin/benchmarks/runs/{id}</c> answers it.</summary>
    [Parameter, EditorRequired] public JsonElement Run { get; set; }

    private RunGraders Graders => RunGradersReader.Read(Run);
}

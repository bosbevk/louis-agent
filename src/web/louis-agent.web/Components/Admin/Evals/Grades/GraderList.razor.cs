using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Admin.Grades;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.Grades;

/// <summary>The run's eval, version, commit and graders, or one line saying it recorded none.</summary>
public partial class GraderList
{
    /// <summary>The run's row, as the contract's stored run reply carries it.</summary>
    [Parameter, EditorRequired]
    public StoredRunRow Run { get; set; } = null!;

    /// <summary>The eval and graders this run recorded.</summary>
    private RunGraders Graders => RunGraders.Of(Run);
}

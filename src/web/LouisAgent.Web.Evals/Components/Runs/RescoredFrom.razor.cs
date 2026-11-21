using LouisAgent.Web.Evals.Admin.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>The link from a re-scored run to the run it re-scored.</summary>
public partial class RescoredFrom
{
    /// <summary>The id of the run that was re-scored.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";
}

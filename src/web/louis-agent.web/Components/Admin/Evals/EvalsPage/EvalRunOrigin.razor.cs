using LouisAgent.Web.Admin.Evals;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.EvalsPage;

/// <summary>
/// A run's tag: recorded live, or played back or graded again from another run, with a link to that run. Only a
/// recorded run measures the model anew, so a person can see which numbers are new.
/// </summary>
public partial class EvalRunOrigin
{
    /// <summary>The run, with where its results came from.</summary>
    [Parameter, EditorRequired]
    public RunLine Line { get; set; } = null!;
}

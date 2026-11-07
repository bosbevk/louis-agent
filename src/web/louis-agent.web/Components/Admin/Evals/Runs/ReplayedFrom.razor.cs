using LouisAgent.Web.Admin.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.Runs;

/// <summary>
/// The link from a replay to the recording it played back (FX28), beside <see cref="RescoredFrom"/> so a row reads
/// the same for both kinds of free run.
/// </summary>
public partial class ReplayedFrom
{
    /// <summary>The id of the run that was replayed.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";
}

using LouisAgent.Api.Contracts.Admin.Studio;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Studio.Check;

/// <summary>
/// The runner's verdict on a draft (F49.5), out of the Studio page so the page stays a list of its steps: nothing
/// before a check, a line when it conforms, else every problem in the runner's words.
/// </summary>
public partial class StudioVerdict
{
    /// <summary>The last verdict the runner gave, or none before a check.</summary>
    [Parameter]
    public DraftCheckReply? Verdict { get; set; }
}

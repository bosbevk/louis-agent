using LouisAgent.Web.Evals.Admin.Studio.Compose;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Studio.Compose;

/// <summary>
/// The head of an eval on the compose page (F49.5): its name, the identity it proves, its version, what a pass means,
/// its manual, and its modes, each changed straight on the eval the page holds.
/// </summary>
/// <remarks>Parameters down: the page owns the eval, a class its inputs bind to (ADR 0040).</remarks>
public partial class ComposeHead
{
    /// <summary>The eval being composed.</summary>
    [Parameter, EditorRequired]
    public EvalComposition Eval { get; set; } = null!;

    /// <summary>Adds an empty mode to fill in.</summary>
    private void AddMode() => Eval.Modes.Add(new ModeEntry());
}

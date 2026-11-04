using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Puls.Admin.Evals;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>
/// What an eval checks, folded away under the runs: most visits are about how the runs went, and this is one click
/// away when you need it.
/// </summary>
/// <remarks>
/// Progressive disclosure (Nielsen Norman Group), chosen in CL26: nine graders one per line pushed the runs out of
/// sight; folded, the card leads with what changes from run to run, and the fold's head still counts what's inside.
/// </remarks>
public partial class EvalChecks
{
    /// <summary>The eval as the API listed it.</summary>
    [Parameter, EditorRequired]
    public EvalRow Eval { get; set; } = null!;

    /// <summary>Its graders, grouped by how they judge.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<GraderGroup> Graders { get; set; } = [];
}

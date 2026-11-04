using LouisAgent.Puls.Admin.Evals.List;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage.List;

/// <summary>One eval's line in the list: a link to its details with its last result.</summary>
public partial class EvalListItem
{
    /// <summary>The line's numbers and words.</summary>
    [Parameter, EditorRequired]
    public EvalLine Line { get; set; } = null!;

    /// <summary>This eval's details are open.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>The line's place in the list, for its entrance delay.</summary>
    [Parameter]
    public int Index { get; set; }
}

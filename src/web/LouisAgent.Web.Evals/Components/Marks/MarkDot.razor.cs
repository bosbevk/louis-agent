using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Marks;

/// <summary>
/// A mark of a fact or a criterion: a tick or a cross, with its words for a screen reader, so a mark is never told
/// by colour alone.
/// </summary>
public partial class MarkDot
{
    /// <summary>Whether the mark was met.</summary>
    [Parameter]
    public bool Met { get; set; }

    /// <summary>What a screen reader says.</summary>
    private string Words => EvalsWords.Words[Met ? EvalsPart.Met : EvalsPart.NotMet];
}

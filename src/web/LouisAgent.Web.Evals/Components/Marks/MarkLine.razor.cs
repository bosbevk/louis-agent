using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs.Answers;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Marks;

/// <summary>
/// One mark of a turn. A fact is checked in code, so it reads as code; a criterion is the judge's, so it carries the
/// judge's line of why.
/// </summary>
public partial class MarkLine
{
    /// <summary>The mark.</summary>
    [Parameter, EditorRequired]
    public Mark Mark { get; set; } = null!;

    /// <summary>Whether it is a fact or a criterion.</summary>
    [Parameter, EditorRequired]
    public MarkKind Kind { get; set; }

    private string KindClass => Kind == MarkKind.Fact ? "mark-line-fact" : "mark-line-criterion";
}

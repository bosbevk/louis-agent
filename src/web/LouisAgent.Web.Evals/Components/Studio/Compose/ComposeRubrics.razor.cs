using LouisAgent.Web.Evals.Admin.Studio.Compose;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Studio.Compose;

/// <summary>
/// The rubrics of an eval on the compose page (F49.5): each a markdown file a model judge reads, by its name in
/// <c>Rubrics/</c>, added, renamed, typed and removed on the list the page holds.
/// </summary>
/// <remarks>Parameters down: the page owns the list, of classes the inputs bind to (ADR 0040).</remarks>
public partial class ComposeRubrics
{
    /// <summary>The eval's rubrics.</summary>
    [Parameter, EditorRequired]
    public List<RubricEntry> Rubrics { get; set; } = null!;

    /// <summary>Adds an empty rubric to fill in.</summary>
    private void Add() => Rubrics.Add(new RubricEntry());
}

using LouisAgent.Api.Contracts.Admin.Reviews;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews.Queue;

/// <summary>
/// The head of the open answer on the Review page (F53.3): the eval, a link to its run, the mode, the repeat and
/// the task; its place in the queue; and Skip, which the page answers.
/// </summary>
public partial class ReviewItemHead
{
    /// <summary>The answer that is open.</summary>
    [Parameter, EditorRequired]
    public ReviewQueueItem Item { get; set; } = null!;

    /// <summary>Its place in the queue, as the page words it.</summary>
    [Parameter, EditorRequired]
    public string Place { get; set; } = "";

    /// <summary>Someone wants the next answer without grading this one.</summary>
    [Parameter]
    public EventCallback OnSkip { get; set; }
}

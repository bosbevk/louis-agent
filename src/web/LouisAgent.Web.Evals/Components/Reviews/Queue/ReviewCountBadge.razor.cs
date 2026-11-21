using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Reviews.Queue;

/// <summary>
/// The count beside Review in the sidebar (F53.3): reads the queue's total once, for the kept name, when the
/// portal opens and nobody has read it yet; then follows <see cref="ReviewQueueState"/>, which the Review page
/// keeps current. An API that refuses (no key yet, no store) shows nothing.
/// </summary>
public partial class ReviewCountBadge : IDisposable
{
    /// <summary>The API the count comes from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>Whose queue: the kept name.</summary>
    [Inject]
    private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>The count, shared with the Review page.</summary>
    [Inject]
    private ReviewQueueState State { get; set; } = null!;

    /// <summary>Listens for the count, then reads it once if nobody has.</summary>
    protected override async Task OnInitializedAsync()
    {
        State.Changed += Redraw;
        await State.ReadOnceAsync(Api, Reviewer);
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => State.Changed -= Redraw;

    /// <summary>The count changed elsewhere.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}

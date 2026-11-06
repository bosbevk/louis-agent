using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews.Queue;

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
        await Reviewer.LoadAsync();
        if (State.Count is not null) return;
        await ReadAsync();
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => State.Changed -= Redraw;

    /// <summary>The queue's total for the kept name, asking for no items.</summary>
    private async Task ReadAsync()
    {
        try
        {
            string who = Reviewer.Name is { } name ? $"reviewer={Uri.EscapeDataString(name)}&" : "";
            var reply = await Api.GetAsync<ReviewQueueReply>($"v1/admin/evals/reviews/queue?{who}limit=1",
                AdminJson.SnakeCase);
            State.Set(reply.Total);
        }
        catch (Exception ex) when (ex is AgentApiException or HttpRequestException)
        {
            // No key yet, or no store: the badge stays away until the Review page sets a count.
        }
    }

    /// <summary>The count changed elsewhere.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}

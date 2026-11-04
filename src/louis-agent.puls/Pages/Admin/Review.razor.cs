using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Review page (F53.3): the answers waiting for this reviewer, one at a time, from the queue the API keeps
/// (<c>GET v1/admin/evals/reviews/queue</c>). A grade or Skip opens the next; past the last one handed over the
/// queue is read again, so the page goes on until nothing waits. The kept name is the queue's: a new name reads
/// it again.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched. The model is <see cref="ReviewQueueView"/>; the panel is the
/// explorer's <c>ReviewPanel</c> unchanged but for the grade it reports, so the blind rule holds here too.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class Review : IDisposable
{
    /// <summary>How many answers the page asks for at a time.</summary>
    private const int Limit = 20;

    /// <summary>The queue as last read, and where the page is in it.</summary>
    private ReviewQueueView? _view;

    /// <summary>Whose queue the page holds, to read it again when the name changes.</summary>
    private string? _loadedFor;

    /// <summary>What went wrong, for the page to say.</summary>
    private string? _error;

    /// <summary>The API wants the admin key.</summary>
    private bool _needsKey;

    /// <summary>The queue is being read.</summary>
    private bool _loading;

    /// <summary>The API the page reads.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The browser's store, loaded before the first read.</summary>
    [Inject]
    private ChatStore Store { get; set; } = null!;

    /// <summary>Who is grading: the kept name.</summary>
    [Inject]
    private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>Loads the key and the name, then the queue; a changed name reads it again.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await Reviewer.LoadAsync();
        // Listen only after the name is in, so its first load isn't read as a change.
        Reviewer.Changed += NameChanged;
        await LoadAsync();
    }

    /// <summary>Stops listening for the name.</summary>
    public void Dispose() => Reviewer.Changed -= NameChanged;

    /// <summary>Reads the queue for the kept name; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        _loadedFor = Reviewer.Name;
        try
        {
            string who = Reviewer.Name is { } name ? $"reviewer={Uri.EscapeDataString(name)}&" : "";
            var reply = await Api.GetAsync<ReviewQueueReply>($"v1/admin/evals/reviews/queue?{who}limit={Limit}",
                AdminJson.SnakeCase);
            _view = ReviewQueueView.Of(reply);
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
    }

    /// <summary>A grade was given: the next answer opens, and past the last one the queue is read again.</summary>
    private Task GradedAsync(string verdict) => MoveAsync(graded: true);

    /// <summary>Skip: the next answer opens without a grade.</summary>
    private Task SkipAsync() => MoveAsync(graded: false);

    /// <summary>Opens the next answer; when every one handed over was opened, reads the queue again.</summary>
    private async Task MoveAsync(bool graded)
    {
        if (_view is null) return;
        _view = _view.Next(graded);
        if (_view.Exhausted) await LoadAsync();
    }

    /// <summary>The kept name changed (typed in the panel, or let go): the queue is the new name's.</summary>
    private void NameChanged()
    {
        if (Reviewer.Name == _loadedFor) return;
        _ = InvokeAsync(async () =>
        {
            await LoadAsync();
            StateHasChanged();
        });
    }

    /// <summary>Which conversation of which run the open answer is.</summary>
    private static ConversationRef Where(ReviewQueueItem item) =>
        new(item.Run, item.Conversation.Mode, item.Conversation.Repeat, item.Conversation.Task);

    /// <summary>The run's review the open answer waits on: the queue only holds answers that have one.</summary>
    private static GraderRow ReviewOf(ReviewQueueItem item) =>
        BlindReview.ReviewOf(item.Graders.Select(GraderRow.Of).ToList(), ConversationGrades.Of(item.Conversation))!;

    /// <summary>One panel per answer: a new key gives a fresh panel, with no grade carried over.</summary>
    private static string Key(ReviewQueueItem item) =>
        $"{item.Run}/{item.Conversation.Mode}/{item.Conversation.Repeat}/{item.Conversation.Task}";
}

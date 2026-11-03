using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Studio;
using LouisAgent.Puls.Admin.Studio.Compose;
using LouisAgent.Puls.Admin.Studio.Schema;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Studio page (F49.5): the evals being composed, and the form for one: its head and modes, its checks picked from
/// the runner's catalogue, its rubrics. It shows the file the form makes, saves it under the reviewer's name, and asks
/// the runner's verdict, every problem in the runner's words.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched and fetches the list again after a save; it never adds its own write
/// to a copy. The form is plain classes (<see cref="EvalComposition"/>), the components change it in place, and the
/// page turns it into the file when it saves, checks or shows it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class EvalStudio : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    private const string Drafts = "v1/admin/evals/drafts";

    private IReadOnlyList<EvalDraftRow> _drafts = [];
    private CheckListReply? _catalogue;
    private EvalComposition? _eval;
    private List<RubricEntry> _rubrics = [];
    private string? _id;
    private DraftCheckReply? _verdict;
    private string? _error;
    private string? _saved;
    private string? _tried;
    private string _tryReplay = "";
    private string _trySamples = "";
    private bool _needsKey, _busy, _showJson;

    /// <summary>The file the form makes now.</summary>
    private string Json => _eval?.ToJson(SchemaFields.ByType(_catalogue?.Checks ?? [])) ?? "";

    /// <summary>The JSON button's words.</summary>
    private string JsonLabel => _showJson ? "Hide JSON" : "View JSON";

    /// <summary>Loads the admin key and the reviewer's name, then the catalogue and the drafts.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the runner's catalogue and the drafts; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _error = null;
        try
        {
            _catalogue = await Api.GetAsync<CheckListReply>("v1/admin/evals/checks", AdminJson.SnakeCase);
            _drafts = (await Api.GetAsync<EvalDraftsReply>(Drafts, AdminJson.SnakeCase)).Drafts;
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
    }

    /// <summary>An empty form for a new eval.</summary>
    private void New() => Show(null, new EvalComposition(), []);

    /// <summary>Opens a draft into the form.</summary>
    private void Open(EvalDraftRow draft) =>
        Show(draft.Id, EvalComposition.FromJson(draft.Json),
            [.. draft.Rubrics.Select(r => new RubricEntry { Name = r.Name, Text = r.Text })]);

    /// <summary>Puts an eval in the form, its verdict and saved line cleared.</summary>
    private void Show(string? id, EvalComposition eval, List<RubricEntry> rubrics)
    {
        (_id, _eval, _rubrics, _verdict, _saved) = (id, eval, rubrics, null, null);
    }

    /// <summary>Saves the form as its draft, new or replaced, and reads the list again.</summary>
    private async Task SaveAsync()
    {
        if (await SaveDraftAsync() is not null) await LoadAsync();
    }

    /// <summary>Saves the draft, then asks the runner's verdict on it.</summary>
    private async Task CheckAsync()
    {
        if (await SaveDraftAsync() is not { } id) return;
        _busy = true;
        try
        {
            using var response = await Api.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"{Drafts}/{Uri.EscapeDataString(id)}/verdict"));
            _verdict = await response.Content.ReadFromJsonAsync<DraftCheckReply>(AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Saves the draft, then starts its try: a replay of the run named, on the samples named, which the Benchmarks
    /// page follows as it runs.
    /// </summary>
    private async Task TryAsync()
    {
        if (await SaveDraftAsync() is not { } id) return;
        _busy = true;
        try
        {
            var samples = _trySamples.Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Drafts}/{Uri.EscapeDataString(id)}/try")
            {
                Content = JsonContent.Create(new TryRequest(_tryReplay.Trim(), samples), options: AdminJson.SnakeCase)
            };
            using var response = await Api.SendAsync(request);
            var run = await response.Content.ReadFromJsonAsync<LiveRunRow>(AdminJson.SnakeCase);
            _tried = StudioText.TryStarted(run!.Id);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Saves the form under the reviewer's name, and gives the draft's id, or none when it couldn't.</summary>
    private async Task<string?> SaveDraftAsync()
    {
        _error = null;
        if (Reviewer.Name is not { } name)
        {
            _error = StudioText.NeedsAName;
            return null;
        }

        _busy = true;
        try
        {
            // A new draft is posted, a kept one replaced under its id; the reply is the draft as kept.
            var body = new EvalDraftRequest(Json, [.. _rubrics.Select(r => new RubricRow(r.Name, r.Text))], name);
            using var request = new HttpRequestMessage(_id is null ? HttpMethod.Post : HttpMethod.Put,
                _id is null ? Drafts : $"{Drafts}/{Uri.EscapeDataString(_id)}")
            {
                Content = JsonContent.Create(body, options: AdminJson.SnakeCase)
            };
            using var response = await Api.SendAsync(request);
            var kept = await response.Content.ReadFromJsonAsync<EvalDraftRow>(AdminJson.SnakeCase);
            _id = kept!.Id;
            _saved = StudioText.EvalSaved(kept.Name);
            return _id;
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
            return null;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Redraws when the reviewer's name changes elsewhere on the page.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    /// <summary>Stops listening for the reviewer's name.</summary>
    public void Dispose() => Reviewer.Changed -= Redraw;
}

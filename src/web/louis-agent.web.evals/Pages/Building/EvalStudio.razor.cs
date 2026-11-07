using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Studio;
using LouisAgent.Web.Evals.Admin.Studio.Compose;
using LouisAgent.Web.Evals.Admin.Studio.Schema;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Evals.Pages.Building;

/// <summary>
/// The Studio page (F49.5): the evals being composed, and the form for one: its head and modes, its checks picked from
/// the runner's catalogue, its rubrics. It shows the file the form makes, saves it under the reviewer's name, and asks
/// the runner's verdict, every problem in the runner's words. The form is four numbered steps in the order they are
/// done, compose, check, try, publish (GOV.UK's task list pattern; the admin review's finding 6).
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched and fetches the list again after a save; it never adds its own write
/// to a copy. The form is plain classes (<see cref="EvalComposition"/>), the components change it in place, and the
/// page turns it into the file when it saves, checks or shows it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class EvalStudio : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private const string Drafts = "v1/admin/evals/drafts";

    private IReadOnlyList<EvalDraftRow> _drafts = [];
    private CheckListReply? _catalogue;
    private EvalComposition? _eval;
    private List<RubricEntry> _rubrics = [];
    private IReadOnlyList<RubricRow> _scenarios = [];
    private string? _id;
    private DraftCheckReply? _verdict;
    private string? _error;
    private string? _saved;
    private string? _published;
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
    private void New() => Show(null, new EvalComposition(), [], []);

    /// <summary>Opens a draft into the form.</summary>
    private void Open(EvalDraftRow draft) =>
        Show(draft.Id, EvalComposition.FromJson(draft.Json),
            [.. draft.Rubrics.Select(r => new RubricEntry { Name = r.Name, Text = r.Text })], draft.Scenarios);

    /// <summary>Puts an eval in the form, its verdict and saved line cleared.</summary>
    private void Show(string? id, EvalComposition eval, List<RubricEntry> rubrics,
        IReadOnlyList<RubricRow> scenarios)
    {
        (_id, _eval, _rubrics, _scenarios, _verdict, _saved) = (id, eval, rubrics, scenarios, null, null);
    }

    /// <summary>Lists the drafts again and opens the one a promptfoo import made.</summary>
    private async Task ImportedAsync(EvalDraftRow draft)
    {
        await LoadAsync();
        Open(draft);
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
    /// Saves the draft, then publishes it: committed on its branch in the workspace, which the page names; or, where
    /// the API has no git workspace, the eval's folder as a zip to download.
    /// </summary>
    private async Task PublishAsync()
    {
        if (await SaveDraftAsync() is not { } id) return;
        _busy = true;
        try
        {
            using var response = await Api.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"{Drafts}/{Uri.EscapeDataString(id)}/publish"));
            if (response.Content.Headers.ContentType?.MediaType == "application/zip")
            {
                // No workspace to commit in: the browser saves the folder as a zip.
                string name = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "eval.zip";
                byte[] zip = await response.Content.ReadAsByteArrayAsync();
                await JS.InvokeVoidAsync("louisDownload", name, Convert.ToBase64String(zip), "application/zip");
                _published = StudioText.PublishedAsZip(name);
                return;
            }

            var published = await response.Content.ReadFromJsonAsync<PublishReply>(AdminJson.SnakeCase);
            _published = StudioText.Published(published!.Branch, published.Folder);
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
            var body = new EvalDraftRequest(Json, [.. _rubrics.Select(r => new RubricRow(r.Name, r.Text))], name,
                _scenarios);
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

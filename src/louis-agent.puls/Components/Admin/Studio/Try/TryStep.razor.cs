using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Puls.Admin.Studio;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Studio.Try;

/// <summary>
/// The Studio's try (F49.5, numbered in F53's leftover): the open draft is saved, then run as a replay of a stored run
/// on a few samples, at no cost. The run is picked from the recorded runs the Evals page lists, the draft's own eval
/// first, and the answer links to the new run's page.
/// </summary>
/// <remarks>
/// Parameters down, events up, as <c>PromptfooPanel</c>: the page owns the draft and its save, so the step asks it to
/// save first. The rows come from <see cref="ReplayChoices"/>, a Presentation Model (Fowler). ADR 0042: the list is
/// what the API answered when the page opened.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class TryStep
{
    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The draft's eval name, whose runs come first in the list.</summary>
    [Parameter]
    public string? Eval { get; set; }

    /// <summary>Saves the open draft and gives its id, or none when it couldn't.</summary>
    [Parameter, EditorRequired]
    public Func<Task<string?>> SaveDraft { get; set; } = null!;

    private const string Drafts = "v1/admin/evals/drafts";

    private IReadOnlyList<EvalRow> _evals = [];
    private string _replay = "", _samples = "";
    private string? _tried, _error;
    private bool _busy, _loaded;

    /// <summary>The runs to pick from, the draft's own eval first.</summary>
    private IReadOnlyList<ReplayChoice> Choices => ReplayChoices.From(_evals, Eval);

    /// <summary>The page of the run a try started.</summary>
    private static string RunLink(string run) => $"admin/runs/{Uri.EscapeDataString(run)}";

    /// <summary>Reads the evals and their runs once, for the picker.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _evals = (await Api.GetAsync<EvalListReply>("v1/admin/evals", AdminJson.SnakeCase)).Evals;
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }

        _loaded = true;
    }

    /// <summary>Saves the draft, then starts its try on the run picked and the samples named.</summary>
    private async Task TryAsync()
    {
        (_error, _tried) = (null, null);
        if (await SaveDraft() is not { } id) return;
        _busy = true;
        try
        {
            var samples = _samples.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Drafts}/{Uri.EscapeDataString(id)}/try")
            {
                Content = JsonContent.Create(new TryRequest(_replay, samples), options: AdminJson.SnakeCase)
            };
            using var response = await Api.SendAsync(request);
            _tried = (await response.Content.ReadFromJsonAsync<LiveRunRow>(AdminJson.SnakeCase))!.Id;
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
}

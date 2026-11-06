using System.Net.Http.Json;
using System.Text;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Studio;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin.Studio.Promptfoo;

/// <summary>
/// promptfoo on the Studio page (F49.5): a pasted promptfoo file is imported by the runner into a new draft, which the
/// page opens, with what had no counterpart listed; the open draft is saved, then exported as a promptfoo file the
/// browser saves.
/// </summary>
/// <remarks>
/// Parameters down, events up: the page owns the drafts and the save, so the panel asks it to save before an export
/// and tells it which draft an import made. The runner does both conversions; the panel only carries the text.
/// </remarks>
public partial class PromptfooPanel
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>Whether a draft is open, so there is one to export.</summary>
    [Parameter]
    public bool HasDraft { get; set; }

    /// <summary>Saves the open draft and gives its id, or none when it couldn't.</summary>
    [Parameter, EditorRequired]
    public Func<Task<string?>> SaveDraft { get; set; } = null!;

    /// <summary>The draft an import made, for the page to list and open.</summary>
    [Parameter]
    public EventCallback<EvalDraftRow> OnImported { get; set; }

    private const string Drafts = "v1/admin/evals/drafts";

    private string _yaml = "";
    private IReadOnlyList<string> _leftOut = [];
    private string? _error, _exported;
    private bool _busy;

    /// <summary>Sends the pasted file to be imported under the reviewer's name, and hands the page its draft.</summary>
    private async Task ImportAsync()
    {
        (_error, _exported, _leftOut) = (null, null, []);
        if (Reviewer.Name is not { } name)
        {
            _error = StudioText.NeedsAName;
            return;
        }

        _busy = true;
        try
        {
            using var response = await Api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{Drafts}/promptfoo")
            {
                Content = JsonContent.Create(new PromptfooImportRequest(_yaml, name), options: AdminJson.SnakeCase)
            });
            var imported = await response.Content.ReadFromJsonAsync<PromptfooImportReply>(AdminJson.SnakeCase);
            _leftOut = imported!.LeftOut;
            await OnImported.InvokeAsync(imported.Draft);
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

    /// <summary>Saves the open draft, then fetches its promptfoo file for the browser to save.</summary>
    private async Task ExportAsync()
    {
        (_error, _exported) = (null, null);
        if (await SaveDraft() is not { } id) return;
        _busy = true;
        try
        {
            using var response = await Api.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{Drafts}/{Uri.EscapeDataString(id)}/promptfoo"));
            string yaml = await response.Content.ReadAsStringAsync();
            await JS.InvokeVoidAsync("louisDownload", ExportName,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(yaml)), "application/yaml");
            _exported = StudioText.PromptfooExported(ExportName);
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

    /// <summary>The name promptfoo looks for its file under.</summary>
    private const string ExportName = "promptfooconfig.yaml";
}

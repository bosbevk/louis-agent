using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Studio;

/// <summary>
/// An eval's drafts on its card: folded under their count, and read from the API only when opened (a transcript is
/// big). Each one can be deleted; after a delete the list is read again and the page told, so the count it shows is
/// the API's, never one kept here.
/// </summary>
/// <remarks>
/// Parameters down, callbacks up: the card hands the eval's name and count, and hears when a draft went (ADR 0042: the
/// web shows the live results).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class DraftList
{
    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The eval whose drafts these are.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>How many drafts the eval list counted.</summary>
    [Parameter]
    public int Count { get; set; }

    /// <summary>A draft was deleted, so the page reads the list again.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private DraftsReply? _drafts;
    private string? _error;
    private bool _open;
    private bool _busy;

    /// <summary>aria-expanded wants the words, and Blazor writes a bool as an empty attribute.</summary>
    private string Expanded => _open ? "true" : "false";

    /// <summary>Points down when open, right when folded.</summary>
    private string Arrow => _open ? "▾" : "›";

    /// <summary>The eval's drafts address, escaped.</summary>
    private string DraftsPath => $"v1/admin/evals/{Uri.EscapeDataString(Eval)}/drafts";

    /// <summary>Opens the list and reads it, or folds it again.</summary>
    private async Task ToggleAsync()
    {
        _open = !_open;
        if (_open) await LoadAsync();
    }

    /// <summary>Reads the eval's drafts.</summary>
    private async Task LoadAsync()
    {
        _error = null;
        try
        {
            _drafts = await Api.GetAsync<DraftsReply>(DraftsPath, AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>Deletes one draft, reads the list again and tells the page.</summary>
    private async Task DeleteAsync(string id)
    {
        _busy = true;
        _error = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{DraftsPath}/{Uri.EscapeDataString(id)}");
            using var _ = await Api.SendAsync(request);
            await LoadAsync();
            await OnChanged.InvokeAsync();
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

using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Prompts;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Prompts page (CL27): every built-in prompt this instance has, as its header says it. It reads the list as the
/// contract's <c>PromptsReply</c>.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched, and Reload fetches again; it keeps no copy of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class Prompts
{
    private IReadOnlyList<PromptRow>? _prompts;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>Loads the admin key, then the list.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the prompts; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _prompts = (await Api.GetAsync<PromptsReply>("v1/admin/prompts", AdminJson.SnakeCase)).Prompts;
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

    /// <summary>A level as the table shows it: a dash for a prompt that names none.</summary>
    private static string Shown(string? level) => level ?? None;

    /// <summary>Toolsets as the table shows them: a comma list, or a dash for none.</summary>
    private static string Shown(IReadOnlyList<string> toolsets) =>
        toolsets.Count == 0 ? None : string.Join(", ", toolsets);

    /// <summary>What a cell shows for nothing.</summary>
    private const string None = "—";
}

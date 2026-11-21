using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Admin;

/// <summary>
/// One row of a pack's screen as its own page, so a long form has room and its address can be kept. It knows no pack:
/// the fields are what the row's description says.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class ScreenRow
{
    private string? _opened, _error;
    private bool _needsKey;
    private ScreenRecordReply? _record;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>The screen's name, from the address.</summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>The row's id, from the address.</summary>
    [Parameter] public string Id { get; set; } = "";

    /// <summary>Back to the screen this row is on.</summary>
    private string BackAddress => $"admin/screens/{Uri.EscapeDataString(Name)}";

    /// <summary>Loads the admin key the first time, then reads whichever row the address names.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if ($"{Name}/{Id}" == _opened)
            return;

        _opened = $"{Name}/{Id}";
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the row; a refused key shows the box to enter one, and a row that's gone says so.</summary>
    private async Task LoadAsync()
    {
        _error = null;
        try
        {
            _record = await Api.GetAsync<ScreenRecordReply>(
                $"v1/admin/screens/{Uri.EscapeDataString(Name)}/rows/{Uri.EscapeDataString(Id)}", AdminJson.SnakeCase);
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
    }
}

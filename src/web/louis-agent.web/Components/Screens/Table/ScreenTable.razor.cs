using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens.Table;

/// <summary>
/// Draws any pack's screen from its description: the search, the rows and the paging. It knows no pack, so a new screen
/// shows here with no change. A view of a pack's own can wrap it and put something above the rows.
/// </summary>
/// <remarks>
/// Metadata-driven UI, as the Django admin draws its lists: the pack says what the columns are and this draws them, so
/// a pack ships no web code for an ordinary table. ADR 0042: it shows what it last read, and keeps no count of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class ScreenTable : IScreenView
{
    private string? _shownScreen, _error;
    private string _searchDraft = "", _search = "";
    private int _pageNumber = 1;
    private bool _needsKey, _loading;
    private ScreenPageReply? _page;
    private ScreenAnswerReply? _answer;

    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The screen to draw.</summary>
    [Parameter, EditorRequired] public ScreenReply Screen { get; set; } = null!;

    /// <summary>What a pack's own view puts above the rows, reading the page they came from.</summary>
    [Parameter] public RenderFragment<ScreenPageReply>? Above { get; set; }

    /// <summary>How many pages there are, for the line between the buttons.</summary>
    private long PageCount => _page is { } page ? (page.Total + page.PageSize - 1) / page.PageSize : 0;

    /// <summary>What sits above the rows, acting on the whole screen.</summary>
    private List<ScreenActionReply> ScreenActions => [.. Screen.Actions.Where(action => action.On == "screen")];

    /// <summary>What sits on each row.</summary>
    private List<ScreenActionReply> RowActions => [.. Screen.Actions.Where(action => action.On == "row")];

    /// <summary>Where a row's form is.</summary>
    private string RowAddress(string rowId) =>
        $"admin/screens/{Uri.EscapeDataString(Screen.Name)}/rows/{Uri.EscapeDataString(rowId)}";

    /// <summary>Says what came of an action, and reads the rows again rather than guessing what changed (ADR 0042).</summary>
    private async Task AfterActionAsync(ScreenAnswerReply answer)
    {
        _answer = answer;
        await LoadAsync();
    }

    /// <summary>A new screen starts over: the page reuses this component when you open the next one.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Screen.Name == _shownScreen)
            return;

        _shownScreen = Screen.Name;
        _searchDraft = _search = "";
        _pageNumber = 1;
        _page = null;
        _answer = null;
        await LoadAsync();
    }

    /// <summary>A search starts from its first page.</summary>
    private async Task SearchAsync()
    {
        _search = _searchDraft.Trim();
        _pageNumber = 1;
        await LoadAsync();
    }

    /// <summary>Goes that many pages on, or back.</summary>
    private async Task TurnAsync(int by)
    {
        _pageNumber += by;
        await LoadAsync();
    }

    /// <summary>Reads the page; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _page = await Api.GetAsync<ScreenPageReply>(Address(), AdminJson.SnakeCase);
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

    /// <summary>Where this page of this search is. The name and the words are escaped for the address.</summary>
    private string Address()
    {
        var address = $"v1/admin/screens/{Uri.EscapeDataString(Screen.Name)}/rows?page={_pageNumber}";
        return _search.Length == 0 ? address : $"{address}&search={Uri.EscapeDataString(_search)}";
    }
}

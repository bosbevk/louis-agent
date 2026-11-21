using LouisAgent.Api.Contracts.Site;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Search;

/// <summary>
/// A site's search (F164.2): a button in its header that opens a window over the page, where the visitor's words find
/// the site's own places as they type, a press opens one at its own address, and the last row asks the site's agent
/// the same words.
/// </summary>
/// <remarks>
/// Louis's pick A of three, drawn as screens: a window, in the look of the chat's own search window, whose rules the
/// two share, over a box in the header with a list under it or a box over the left menu. The places come from the
/// host's index (<c>GET /v1/site/places</c>) with no model, and only the last row spends a model call, put through the
/// door as every Ask button's is (Observer). Its keys are held by the framework's shortcut module (ADR 0093).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public partial class SiteSearch : IAsyncDisposable
{
    /// <summary>The fewest letters the host searches, as its search counts a word.</summary>
    private const int FewestLetters = 2;

    /// <summary>The button's words, and the window's name.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>What the box says before the visitor types.</summary>
    [Parameter, EditorRequired]
    public string Placeholder { get; set; } = "";

    /// <summary>The last row's words, such as <c>Ask Orbi about "{0}"</c>; {0} the visitor's words.</summary>
    [Parameter, EditorRequired]
    public string AskAbout { get; set; } = "";

    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The site's words for the window's foot.</summary>
    [Inject]
    private ChatText Words { get; set; } = default!;

    private ElementReference _button;
    private ElementReference _input;
    private JsModule? _module;
    private bool _open;
    private bool _wasOpen;
    private string _query = "";
    private IReadOnlyList<PlaceFound> _places = [];
    private int _selected;
    private bool _handled;

    /// <summary>How many searches were asked, so the answer to older words, come late, is let go.</summary>
    private int _asked;

    /// <summary>The visitor's words, without the space around them.</summary>
    private string Typed => _query.Trim();

    /// <summary>Whether the visitor has typed anything, so the window shows its rows.</summary>
    private bool HasWords => Typed.Length > 0;

    /// <summary>The last row's words, with the visitor's in them.</summary>
    private string AskWords => string.Format(AskAbout, Typed);

    /// <summary>The class a row takes when it is the one chosen.</summary>
    private string Selected(int index) => index == _selected ? "selected" : "";

    /// <summary>Opens the window, empty, as it opens each time.</summary>
    private void OpenWindow()
    {
        _open = true;
        _query = "";
        _places = [];
        _selected = 0;
    }

    /// <summary>Hands the button its keys once drawn, and puts the cursor in the box as the window opens.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = new JsModule(JS, FrameworkScripts.Shortcut);
            await _module.InvokeVoidAsync("attach", _button, "k");
        }

        if (_open && !_wasOpen) await _input.FocusAsync();
        _wasOpen = _open;
    }

    /// <summary>
    /// Asks the host for the places that hold the words; an answer that comes after newer words were typed is let go,
    /// and a failed search shows none, so the visitor can still ask the agent.
    /// </summary>
    private async Task FindAsync()
    {
        _selected = 0;
        int asked = ++_asked;
        if (Typed.Length < FewestLetters)
        {
            _places = [];
            return;
        }

        IReadOnlyList<PlaceFound> found;
        try
        {
            string address = $"v1/site/places?q={Uri.EscapeDataString(Typed)}";
            found = (await Api.GetAsync<PlacesReply>(address)).Places;
        }
        catch (AgentApiException)
        {
            found = [];
        }

        if (asked == _asked) _places = found;
    }

    /// <summary>The arrow keys move the choice, Enter opens it, and Escape closes the window.</summary>
    private void OnKeyDown(KeyboardEventArgs e)
    {
        _handled = e.Key is "ArrowDown" or "ArrowUp" or "Enter";
        switch (e.Key)
        {
            case "Escape": Close(); break;
            case "ArrowDown": _selected = Math.Min(_selected + 1, _places.Count); break;
            case "ArrowUp": _selected = Math.Max(_selected - 1, 0); break;
            case "Enter" when HasWords: Choose(); break;
        }
    }

    /// <summary>Opens the chosen place, or asks the agent when the last row is chosen.</summary>
    private void Choose()
    {
        if (_selected < _places.Count) Go(_places[_selected]);
        else Ask();
    }

    /// <summary>Opens a place at its own address, and closes the window.</summary>
    private void Go(PlaceFound place)
    {
        Close();
        Nav.NavigateTo(place.Address);
    }

    /// <summary>Asks the site's agent the visitor's words through the door, and closes the window.</summary>
    private void Ask()
    {
        string words = Typed;
        Close();
        Services.GetService<GuideDoor>()?.Ask(words);
    }

    /// <summary>Closes the window.</summary>
    private void Close() => _open = false;

    /// <summary>Lets the button's keys go with it, and the module with them.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("detach", _button);
        await _module.DisposeAsync();
    }
}

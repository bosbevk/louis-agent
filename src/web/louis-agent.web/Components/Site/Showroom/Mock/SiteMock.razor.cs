using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Showroom.Mock.Website;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// A mock of a business's own website (F127.1, Louis's pick 3a, made fuller): a browser bar with its address, its name
/// and menu in its brand colour, then a page that scrolls inside the mock (on home a hero in its look and a strip of
/// facts, then its pages, F127.2, only the one it's on showing), and its agent chatting in a widget in the corner. A
/// visitor sees the agent where its customers would meet it, on a website like their own, and can click through it.
/// </summary>
/// <remarks>
/// Device mock-up, as product pages show an app in a phone or a browser (the research: Copilot Studio's demo website,
/// Gardenia's "how their environment would look"). The business's sample brand is the page's data (its colours as
/// <see cref="MockColours"/>, set on the mock as variables its styles read, its mark, tagline, <see cref="MockFont"/>
/// and <see cref="MockLook"/>), so its look is its own and never the site's theme. The page keeps clear of the widget
/// and lays itself out by the mock's own width, not the screen's, since the stage beside the rail is narrower than
/// the screen. The place its agent shows is handed down to the places as a cascading value, so a site writes its
/// places once and never says which is lit; the mock scrolls its own page there through a module beside it. Its pages
/// register with it as Microsoft's TabSet example has its tabs do (<see cref="MockPage"/>), and the page it's on is
/// handed down the same way. Where it is has two sources, its agent and a visitor's click; the agent's next place
/// wins, so the recording and the visitor never fight over the website.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class SiteMock : IAsyncDisposable
{
    /// <summary>The name its places know the shown place by, handed down to them.</summary>
    public const string ShownPlace = "MockShown";

    /// <summary>The name its pages know the page it's on by, handed down to them.</summary>
    public const string CurrentPageName = "MockCurrentPage";

    private readonly MockSiteMap _map = new();
    private JsModule? _module;
    private ElementReference _page;
    private string? _scrolledTo;
    private string? _went;
    private string? _shownBefore;
    private string? _pageDrawn;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The business's address in the browser bar: <c>the-bakery.example</c>.</summary>
    [Parameter, EditorRequired]
    public string Address { get; set; } = "";

    /// <summary>The business's name, at the start of its header.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>The business's own colours.</summary>
    [Parameter, EditorRequired]
    public MockColours Colours { get; set; } = null!;

    /// <summary>Its logo mark, drawn for it in SVG (its sample brand); none for a business with no mark.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The few words under its name; none for a business with no tagline.</summary>
    [Parameter]
    public string? Tagline { get; set; }

    /// <summary>The typeface its website wears; the serif unless it has its own.</summary>
    [Parameter]
    public MockFont Font { get; set; }

    /// <summary>The words of its menu.</summary>
    [Parameter]
    public IReadOnlyList<string> NavItems { get; set; } = [];

    /// <summary>Its hero's line: "Fresh bread before seven."</summary>
    [Parameter, EditorRequired]
    public string HeroLine { get; set; } = "";

    /// <summary>A few words under the hero's line; none for a hero of one line.</summary>
    [Parameter]
    public string? HeroNote { get; set; }

    /// <summary>Its look: its words beside its scene unless it has a look of its own.</summary>
    [Parameter]
    public MockLook Look { get; set; }

    /// <summary>Its hero's scene, drawn for it in SVG; none for a hero of words alone.</summary>
    [Parameter]
    public RenderFragment? Scene { get; set; }

    /// <summary>
    /// A real photo for its hero, from the site's own address (<c>images/showroom/bakery.webp</c>), worn with its
    /// brand; the scene when it has none.
    /// </summary>
    [Parameter]
    public string? HeroPhoto { get; set; }

    /// <summary>What the hero's photo shows, for a screen reader.</summary>
    [Parameter]
    public string HeroPhotoAlt { get; set; } = "";

    /// <summary>Its hero's call to action: "Book a table"; none for a hero without one.</summary>
    [Parameter]
    public string? HeroAction { get; set; }

    /// <summary>A few facts in a strip under its hero: "Open from six".</summary>
    [Parameter]
    public IReadOnlyList<string> Facts { get; set; } = [];

    /// <summary>Its places, under the facts: its sections and cards, each with its own address.</summary>
    [Parameter]
    public RenderFragment? Places { get; set; }

    /// <summary>
    /// Its pages (F127.2): each a <see cref="MockPage"/> with its address, its menu word and its sections, in the order
    /// its menu reads.
    /// </summary>
    [Parameter]
    public RenderFragment? Pages { get; set; }

    /// <summary>Told where a visitor's click went, by the page's address, so the site can follow.</summary>
    [Parameter]
    public EventCallback<string> Went { get; set; }

    /// <summary>
    /// The place its agent showed, by its address on the business's site (<c>/rooms/garden</c>); none at home.
    /// </summary>
    [Parameter]
    public string? Shown { get; set; }

    /// <summary>The widget's title: "The Bakery · ask us".</summary>
    [Parameter]
    public string AgentLabel { get; set; } = "";

    /// <summary>The agent: its recorded chat.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Agent { get; set; } = null!;

    /// <summary>The module beside the mock that scrolls its page, imported the first time a place is shown.</summary>
    private JsModule Module => _module ??= new JsModule(JS, FrameworkScripts.Mock);

    /// <summary>
    /// Where the website is: where a visitor's click went, else the place its agent showed; none at home.
    /// </summary>
    private string? Here => _went ?? Shown;

    /// <summary>The page the website is on.</summary>
    private string CurrentPage => _map.PageOf(Here);

    /// <summary>Whether the hero shows: on home, or on a site of one long page.</summary>
    private bool ShowsHome => !_map.Any || CurrentPage == MockPaths.Home;

    /// <summary>The pages its menu lists.</summary>
    private IEnumerable<MockPage> MenuPages => _map.Menu;

    /// <summary>A menu word's classes: the page the website is on is marked.</summary>
    private string NavClass(MockPage page) => page.Path == CurrentPage ? "mock-nav-item is-current" : "mock-nav-item";

    /// <summary>The current page's menu word says so, for a screen reader.</summary>
    private string? CurrentMark(MockPage page) => page.Path == CurrentPage ? "page" : null;

    /// <summary>A page says it's there; the menu draws again with it.</summary>
    internal void Register(MockPage page)
    {
        if (_map.Add(page)) StateHasChanged();
    }

    /// <summary>A page goes.</summary>
    internal void Unregister(MockPage page) => _map.Remove(page);

    /// <summary>A page's word in the menu, for a way back to it.</summary>
    internal string? LabelOf(string? path) => _map.LabelOf(path);

    /// <summary>A visitor's click: the website goes there, and the site hears of it.</summary>
    internal async Task GoAsync(string path)
    {
        _went = path;
        StateHasChanged();
        await Went.InvokeAsync(path);
    }

    /// <summary>A place its agent newly showed wins over where a visitor's click went.</summary>
    protected override void OnParametersSet()
    {
        if (Shown == _shownBefore) return;
        _shownBefore = Shown;
        _went = null;
    }

    /// <summary>
    /// On a new page the mock's own page goes to its top, then to a newly shown place, once a render has drawn it lit.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The first page drawn is where the mock starts: nothing to scroll from.
        string page = CurrentPage;
        if (firstRender) _pageDrawn = page;
        if (page != _pageDrawn)
        {
            _pageDrawn = page;
            await Module.InvokeVoidAsync("toTop", _page);
        }

        if (Shown == _scrolledTo) return;
        _scrolledTo = Shown;
        if (Shown is not null) await Module.InvokeVoidAsync("showPlace", _page, Shown);
    }

    /// <summary>Lets the module go with the mock.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>The mock's classes: a mock, in its business's typeface and look.</summary>
    private string Classes => $"site-mock font-{Font.Wire()} look-{Look.Wire()}";

    /// <summary>The business's colours as the variables the mock's styles read.</summary>
    private string ColourStyle =>
        $"--mock-brand: {Colours.Brand}; --mock-on-brand: {Colours.OnBrand}; --mock-page: {Colours.Page}; " +
        $"--mock-ink: {Colours.Ink}";
}

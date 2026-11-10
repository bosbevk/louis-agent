using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// A chat any page or layout drops in with one tag (F109.3): a button folded in a corner, a panel with the chat's
/// own list and composer, opened as the identity the page names, on a runner and a store of its own.
/// </summary>
/// <remarks>
/// Facade, in Razor (Gang of Four): the chat frame's parts (the list, the composer, the notice) and the runner that
/// gives a turn its life are the same ones <c>ChatShell</c> is built from, behind one small tag with the identity as
/// its one setting; nothing of the chat is written twice. Its store keeps its chats under a key of its own, so a
/// site's chat page never lists the widget's conversations.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/facade">Facade (Refactoring.Guru)</seealso>
public partial class ChatWidget : IAsyncDisposable
{
    /// <summary>Where the widget's own chats are kept, apart from the chat page's.</summary>
    private const string ChatsKey = "louis-agent.widget-chats";

    private bool _open;
    private ChatWindowSize _size;
    private JsModule? _module;
    private string? _pageSentFor;

    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ChatSettings Settings { get; set; } = default!;
    [Inject] private CurrencyState Currency { get; set; } = default!;
    [Inject] private ChatText Text { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>Who answers: the identity a session opens as, by the name the host's catalogue knows.</summary>
    [Parameter, EditorRequired]
    public string Identity { get; set; } = "";

    /// <summary>
    /// The toolsets the session asks for; none by default, since the identity's own packs come with it.
    /// </summary>
    [Parameter]
    public IEnumerable<string> Toolsets { get; set; } = [];

    /// <summary>The panel's title.</summary>
    [Parameter]
    public string Title { get; set; } = "Ask";

    /// <summary>What the panel says before the first question.</summary>
    [Parameter]
    public string Greeting { get; set; } = "Ask me anything.";

    /// <summary>The composer's placeholder.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "Ask a question…";

    /// <summary>What the folded button says.</summary>
    [Parameter]
    public string ButtonText { get; set; } = "?";

    /// <summary>The button's label while the panel is folded.</summary>
    [Parameter]
    public string OpenLabel { get; set; } = "Open the chat";

    /// <summary>The button's label while the panel is open.</summary>
    [Parameter]
    public string CloseLabel { get; set; } = "Close the chat";

    /// <summary>
    /// The words on the page's tour button, alone in the folded corner since FX114 (F118); none, and there is no tour.
    /// </summary>
    [Parameter]
    public string? TourLabel { get; set; }

    /// <summary>What the tour asks the guide.</summary>
    [Parameter]
    public string TourQuestion { get; set; } = "Walk me through this page, one section at a time.";

    /// <summary>
    /// The words on the tour's button once a conversation has started (FX117), which picks it up again; none, and the
    /// tour always starts afresh.
    /// </summary>
    [Parameter]
    public string? ResumeLabel { get; set; }

    /// <summary>
    /// What resuming tells the guide; the page the visitor is on now goes with it, as with every message.
    /// </summary>
    [Parameter]
    public string ResumeQuestion { get; set; } = "Let's carry on with the tour.";

    /// <summary>
    /// Whether the chat shows the agent's work, what it thought and the tools it called (F128.1). A site's visitor
    /// may want the answer alone: off, they see its words and a thinking label.
    /// </summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>A conversation has started and the site offers to pick it up again.</summary>
    private bool CanResume => ResumeLabel is not null && Runner.Chat is { Turns.Count: > 0 };

    private ChatRunner Runner { get; set; } = default!;

    /// <summary>The module beside this component that reads the page (F115), imported on first use.</summary>
    private JsModule Module => _module ??= new JsModule(JS, FrameworkScripts.Widget);

    /// <summary>The runner over the site's API, on a store of its own, opening every session as the identity.</summary>
    protected override void OnInitialized()
    {
        Runner = new ChatRunner(Api, new ChatStore(JS, Settings, ChatsKey), Text, OpenSessionAsync);
        Runner.Changed += Redraw;
        Runner.ToolCalled += OnToolCalled;
        Currency.Changed += Redraw;
        if (Door is { } door) door.Asked += OnAsked;
    }

    /// <summary>The way in from a button anywhere on the site (F118), when the site has one.</summary>
    private GuideDoor? Door => Services.GetService<GuideDoor>();

    /// <summary>
    /// A button asked the guide a question: the panel opens and it goes as if the visitor had typed it. A minimised
    /// panel comes back, so the answer is never hidden (F127.2).
    /// </summary>
    private void OnAsked(string question) => InvokeAsync(async () =>
    {
        _open = true;
        if (_size == ChatWindowSize.Minimised) _size = ChatWindowSize.Normal;
        Runner.Draft = question;
        StateHasChanged();
        await SendAsync();
    });

    /// <summary>
    /// The guide takes the visitor to a place (F117, ADR 0107): on a ShowPage call the address is read into the page
    /// and the place on it; the browser goes to the page only when it is another one, and the module scrolls to the
    /// place, opens a card or turns a paged section, and lights it once the page has it. Any other tool is the chat's.
    /// </summary>
    private void OnToolCalled(Block block)
    {
        if (ShowPageCall.PathOf(block) is not { } path || Addresses is not { } addresses) return;
        if (GuidePlaces.Target(path, addresses) is not { } target) return;

        if (target.Page != PageAddress) Nav.NavigateTo(addresses.Address(target.Page, null));
        // The bar shows the place's address as the rule writes it, so /parts/1 shows as /parts.
        if (target.Place is not null)
            _ = Module.InvokeVoidAsync("showArea", target.Place, addresses.Address(target.Page, null),
                GuidePlaces.AddressOf(target.Place, addresses, target.Page));
    }

    /// <summary>The site's addresses (ADR 0107), when the site has them.</summary>
    private SiteAddresses? Addresses => Services.GetService<SiteAddresses>();

    /// <summary>The page the visitor is on, without the place on it.</summary>
    private string PageAddress => Addresses?.Read(Nav.Uri)?.Page ?? new Uri(Nav.Uri).AbsolutePath;

    /// <summary>
    /// Asks the API for the rates and the display currency once, so a turn's cost shows in the site's currency, not as
    /// charged (FX80, Louis: "cost in rand"); the chat page does the same through its currency switcher.
    /// </summary>
    protected override Task OnInitializedAsync() => Currency.LoadAsync();

    public async ValueTask DisposeAsync()
    {
        Runner.Changed -= Redraw;
        Runner.ToolCalled -= OnToolCalled;
        Currency.Changed -= Redraw;
        if (Door is { } door) door.Asked -= OnAsked;
        if (_module is not null) await _module.DisposeAsync();
    }

    /// <summary>
    /// Sends the message, with the page the visitor is reading when this page hasn't gone with a message yet (F115):
    /// read at send time, so it is the page as rendered, not the one before a move. Every message carries the section
    /// on their screen (F118, Louis: "see it did not know where I was") and the card open there (FX106), so "this" is
    /// what they look at; a later one from the same page carries only those, so the page's words go once.
    /// </summary>
    private async Task SendAsync()
    {
        var inView = await Module.InvokeAsync<PageArea?>("inView");
        var open = await Module.InvokeAsync<PageArea?>("openCard");
        string here = PageAddress;
        Page? page = null;
        if (_pageSentFor != here)
        {
            page = await Module.InvokeAsync<Page?>("pageText");
            page = page is null
                ? null
                : page with
                {
                    InView = inView,
                    Open = open
                };
            _pageSentFor = here;
        }
        else if (inView is not null || open is not null)
        {
            page = new Page(here, "", "", [], inView, open);
        }

        // Every place by its own address (ADR 0107), so the guide names what it sends the visitor to as it reads it.
        Runner.Page = page is not null && Addresses is { } addresses
            ? GuidePlaces.Addressed(page, addresses, here)
            : page;
        await Runner.SendAsync();
    }

    private Task<string> OpenSessionAsync() => Api.CreateSessionAsync(Toolsets, identity: Identity);

    /// <summary>Opens or folds the panel; a panel that opens again comes as it first did.</summary>
    private void Toggle()
    {
        _open = !_open;
        _size = ChatWindowSize.Normal;
    }

    /// <summary>The panel's classes: its size, as the styles read it (F127.2).</summary>
    private string PanelClasses => _size switch
    {
        ChatWindowSize.Minimised => "widget-panel is-min",
        ChatWindowSize.Maximised => "widget-panel is-max",
        _ => "widget-panel",
    };

    private void Redraw() => InvokeAsync(StateHasChanged);
}

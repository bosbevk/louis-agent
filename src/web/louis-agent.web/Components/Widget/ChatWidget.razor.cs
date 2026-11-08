using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
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
    private JsModule? _module;
    private string? _pageSentFor;

    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ChatSettings Settings { get; set; } = default!;
    [Inject] private CurrencyState Currency { get; set; } = default!;
    [Inject] private ChatText Text { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>Who answers: the identity a session opens as, by the name the host's catalogue knows.</summary>
    [Parameter, EditorRequired]
    public string Identity { get; set; } = "";

    /// <summary>The toolsets the session asks for; none by default, since the identity's own packs come with it.</summary>
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

    private ChatRunner Runner { get; set; } = default!;

    /// <summary>The module beside this component that reads the page (F115), imported on first use.</summary>
    private JsModule Module => _module ??= new JsModule(JS, FrameworkScripts.Widget);

    /// <summary>The runner over the site's API, on a store of its own, opening every session as the identity.</summary>
    protected override void OnInitialized()
    {
        Runner = new ChatRunner(Api, new ChatStore(JS, Settings, ChatsKey), Text, OpenSessionAsync);
        Runner.Changed += Redraw;
        Currency.Changed += Redraw;
    }

    /// <summary>
    /// Asks the API for the rates and the display currency once, so a turn's cost shows in the site's currency, not as
    /// charged (FX80, Louis: "cost in rand"); the chat page does the same through its currency switcher.
    /// </summary>
    protected override Task OnInitializedAsync() => Currency.LoadAsync();

    public async ValueTask DisposeAsync()
    {
        Runner.Changed -= Redraw;
        Currency.Changed -= Redraw;
        if (_module is not null) await _module.DisposeAsync();
    }

    /// <summary>
    /// Sends the message, with the page the visitor is reading when this page hasn't gone with a message yet (F115):
    /// read at send time, so it is the page as rendered, not the one before a move.
    /// </summary>
    private async Task SendAsync()
    {
        if (_pageSentFor != Nav.Uri)
        {
            Runner.Page = await Module.InvokeAsync<Page>("pageText");
            _pageSentFor = Nav.Uri;
        }

        await Runner.SendAsync();
    }

    private Task<string> OpenSessionAsync() => Api.CreateSessionAsync(Toolsets, identity: Identity);

    private void Toggle() => _open = !_open;

    private void Redraw() => InvokeAsync(StateHasChanged);
}

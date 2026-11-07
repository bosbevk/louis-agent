using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Shell;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Shell;

/// <summary>
/// The chat's whole frame, the same on every site. A site writes a small page around it: its logo, its views, what an
/// empty chat shows, and how a session opens, since the toolsets it asks for are its own.
/// </summary>
/// <remarks>
/// Template Method, in Razor: the frame fixes the order of the parts, and the slots are the steps a site fills in.
/// Without it each site keeps its own copy of about 300 lines, and they drift apart. The small state classes it holds
/// are Presentation Models, so the rules can be tested without drawing anything.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class ChatShell : IAsyncDisposable
{
    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private ChatStore Store { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The framework's dialogs module, for the rename box's focus (CL31).</summary>
    private JsModule Dialogs => _dialogs ??= new JsModule(JS, FrameworkScripts.Dialogs);

    private JsModule? _dialogs;
    private JsModule? _preferencesModule;
    private JsModule? _shell;

    /// <summary>The shell's own module (CL31): the page-wide behaviour, attached while the shell is drawn.</summary>
    private JsModule Shell => _shell ??= new JsModule(JS, FrameworkScripts.Shell);

    [Inject] private CurrencyState Currency { get; set; } = default!;
    [Inject] private ChatText Text { get; set; } = default!;

    /// <summary>The chat the address names; null for a new one.</summary>
    [Parameter]
    public string? SessionId { get; set; }

    /// <summary>Where this site keeps its chats, such as <c>chat</c> for <c>chat/{id}</c>.</summary>
    [Parameter]
    public string ChatPath { get; set; } = "chat";

    /// <summary>The page's title when no chat is open.</summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>How this site opens a session, since the toolsets it asks for are its own.</summary>
    [Parameter, EditorRequired]
    public Func<ChatOpening, Task<string>> OpenSession { get; set; } = default!;

    /// <summary>What comes before a language's code in this site's voices.</summary>
    [Parameter, EditorRequired]
    public string VoicePrefix { get; set; } = "";

    /// <summary>The language a new chat speaks unless this browser chose another.</summary>
    [Parameter, EditorRequired]
    public string DefaultLanguage { get; set; } = "";

    /// <summary>The site's logo, in the phone's top bar and the sidebar.</summary>
    [Parameter]
    public RenderFragment? Logo { get; set; }

    /// <summary>The line under the logo in the sidebar.</summary>
    [Parameter]
    public string? Tagline { get; set; }

    /// <summary>What the logo says when you hover over it.</summary>
    [Parameter]
    public string? BrandTitle { get; set; }

    /// <summary>The site's own links in the sidebar.</summary>
    [Parameter]
    public RenderFragment? Views { get; set; }

    /// <summary>Who's signed in, above the settings.</summary>
    [Parameter]
    public RenderFragment? Profile { get; set; }

    /// <summary>A view the site shows in place of the chat; with none, the chat shows.</summary>
    [Parameter]
    public RenderFragment? ViewContent { get; set; }

    /// <summary>That view's title in the top bar.</summary>
    [Parameter]
    public string? ViewTitle { get; set; }

    /// <summary>What sits behind an empty chat.</summary>
    [Parameter]
    public RenderFragment? Background { get; set; }

    /// <summary>What an empty chat shows; with none, just the message box.</summary>
    [Parameter]
    public RenderFragment<ChatShellEmpty>? Empty { get; set; }

    /// <summary>The site's own words for the line with a chat's cost; with none, the framework's.</summary>
    [Parameter]
    public RenderFragment<TurnUsage>? UsageLine { get; set; }

    /// <summary>The parts this site turns off; every one is on unless it says otherwise.</summary>
    [Parameter]
    public ChatFeatures Features { get; set; } = new();

    /// <summary>The site's own settings, with the frame's.</summary>
    [Parameter]
    public RenderFragment? Settings { get; set; }

    /// <summary>The site's picture for an empty work panel.</summary>
    [Parameter]
    public string? EmptyWorkImage { get; set; }

    /// <summary>The site's picture beside each piece of work.</summary>
    [Parameter]
    public string? WorkAvatar { get; set; }

    private ChatRunner Runner { get; set; } = default!;

    private ChatLanguages _languages = null!;
    private ChatRenaming _renaming = null!;
    private ChatPreferences _preferences = null!;
    private string? _model;
    private string? _shownSession;
    private string? _toast;
    private bool _searchOpen;
    private bool _focusDraft;

    private bool ChatIsEmpty => Runner.Chat is null || Runner.Chat.Turns.Count == 0;

    private TurnUsage? ChatUsage =>
        ViewContent is null && Runner.Chat is not null ? ChatCosts.Total(Runner.Chat) : null;

    /// <summary>The chats used this calendar month, in this browser.</summary>
    private List<ChatModel> MonthChats => ChatCosts.InMonth(Store.Chats, TimeProvider.System.GetLocalNow());

    private string MonthCost => ChatCosts.Say(ChatCosts.Total(MonthChats), Currency.Current);

    private string ChatCost =>
        Runner.Chat is null ? "—" : ChatCosts.Say(ChatCosts.Total(Runner.Chat), Currency.Current);

    /// <summary>Sets up the runner, listens for changes, and loads what the frame needs.</summary>
    protected override async Task OnInitializedAsync()
    {
        Runner = new ChatRunner(Api, Store, Text, OpenSessionAsync);
        _renaming = new ChatRenaming(Store);
        // One preferences module for both (CL31): what this browser remembers for the person.
        _preferencesModule = new JsModule(JS, FrameworkScripts.Preferences);
        _preferences = new ChatPreferences(_preferencesModule, Text);
        _languages = new ChatLanguages(Api, _preferencesModule, VoicePrefix, DefaultLanguage);
        Runner.Changed += Redraw;
        Runner.SessionOpened += id => Nav.NavigateTo($"{ChatPath}/{id}");
        Currency.Changed += Redraw;
        if (!Store.Loaded) await Store.LoadAsync();
        await _preferences.LoadAsync();
        // With no language menu there's no list to read, and no choice to read back.
        if (Features.Language) await _languages.LoadAsync();

        try
        {
            var model = await Api.GetAsync<System.Text.Json.JsonElement>("v1/model");
            _model = Pretty(model.GetProperty("model").GetString());
        }
        catch (Exception)
        {
            _model = null; // An API that can't say: the picker says "Model".
        }
    }

    /// <summary>Opens the chat the address names.</summary>
    protected override void OnParametersSet()
    {
        // A site's page redraws for its own reasons, which sets these again: only a new address opens another chat.
        if (SessionId == _shownSession) return;
        _shownSession = SessionId;

        var chat = Store.Find(SessionId);
        Runner.Chat = chat;
        if (SessionId is not null && chat is null && !Runner.Streaming) Runner.Notice = Text.ChatNotSaved;
    }

    /// <summary>Does what had to wait for the draw, such as putting the cursor in the message box.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The listeners the page hangs on the document, while the shell is drawn.
        if (firstRender) await Shell.InvokeVoidAsync("attach");

        if (_focusDraft)
        {
            _focusDraft = false;
            await Shell.InvokeVoidAsync("focusEnd", "#composer-input");
        }

        if (_renaming.TakeFocus())
        {
            await Dialogs.InvokeVoidAsync("focus", "#rename-input", true);
        }

        // A cost that changed since the last draw counts up to its new value (data-count elements).
        await Shell.InvokeVoidAsync("countCosts");
    }

    /// <summary>Stops listening when the frame goes, takes the page's listeners down, and lets the modules go if they were ever loaded.</summary>
    public async ValueTask DisposeAsync()
    {
        Runner.Changed -= Redraw;
        Currency.Changed -= Redraw;
        // A closed browser tab has no JS runtime to tell.
        try
        {
            if (_shell is not null) await _shell.InvokeVoidAsync("detach");
        }
        catch (JSDisconnectedException)
        {
        }

        await Release(_dialogs, _preferencesModule, _shell);
    }

    /// <summary>Lets every module go that was ever loaded.</summary>
    private static async ValueTask Release(params JsModule?[] modules)
    {
        foreach (var module in modules)
            if (module is not null)
                await module.DisposeAsync();
    }

    /// <summary>A placeholder's answer: a short word saying what's coming.</summary>
    public Task ComingSoonAsync(string what) => ShowToast(string.Format(Text.ComingSoon, what));

    /// <summary>Puts this text in the message box, for the person to finish and send.</summary>
    public Task SetDraftAsync(string text)
    {
        Runner.Draft = text;
        // Focused and grown once the box shows the text (after the draw, possibly on another view's page).
        _focusDraft = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>A new chat's session, the site's way, with what only the frame knows.</summary>
    private async Task<string> OpenSessionAsync()
    {
        string? timeZone = await Shell.InvokeAsync<string?>("timeZone");
        return await OpenSession(new ChatOpening(_languages.Role, _preferences.Location, timeZone));
    }

    /// <summary>What a site's empty chat is handed.</summary>
    private ChatShellEmpty EmptyWith(RenderFragment messageBox) => new(
        messageBox,
        Runner.Streaming,
        EventCallback.Factory.Create<string>(this, SetDraftAsync),
        EventCallback.Factory.Create<string>(this, Suggest));

    /// <summary>Something the frame shows changed, so draw again.</summary>
    private void Redraw() => InvokeAsync(StateHasChanged);

    /// <summary>Goes back to an empty chat.</summary>
    private void NewChat()
    {
        Runner.Notice = null;
        Nav.NavigateTo(ChatPath);
    }

    /// <summary>Sends a suggested question, unless a turn is already running.</summary>
    private async Task Suggest(string prompt)
    {
        if (Runner.Streaming) return;
        await Runner.StartTurnAsync(prompt, []);
    }

    /// <summary>Kept in this browser; the next chat opens in it, and the open one keeps its voice.</summary>
    private Task ChooseLanguage(string code) => _languages.ChooseAsync(code);

    /// <summary>Copies the text and says so.</summary>
    private async Task Copy(string text)
    {
        bool copied = await Shell.InvokeAsync<bool>("copy", text);
        _ = ShowToast(copied ? Text.Copied : Text.CopyFailed);
    }

    /// <summary>A model's id as a name: <c>claude-opus-5-5</c> reads Claude Opus 5.5.</summary>
    private static string? Pretty(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var words = id.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var named = words.TakeWhile(w => !char.IsDigit(w[0])).Select(w => char.ToUpperInvariant(w[0]) + w[1..]);
        string version = string.Join(".", words.SkipWhile(w => !char.IsDigit(w[0])));
        return string.Join(" ", named) + (version.Length > 0 ? " " + version : "");
    }

    /// <summary>Keeps the place the person typed.</summary>
    private Task SaveLocation(string? value) => _preferences.SaveLocationAsync(value);

    /// <summary>Keeps whether the page may move.</summary>
    private Task SaveMotion(bool motion) => _preferences.SaveMotionAsync(motion);

    /// <summary>Asks the browser where we are and keeps the place, or says why it can't.</summary>
    private async Task UseMyLocation() => _ = ShowToast(await _preferences.UseMyLocationAsync());

    /// <summary>Shows a short message, then takes it away.</summary>
    private async Task ShowToast(string text)
    {
        _toast = text;
        StateHasChanged();
        await Task.Delay(2800);
        if (_toast != text) return;
        _toast = null;
        StateHasChanged();
    }

    /// <summary>Opens the rename field, in the sidebar or the header.</summary>
    private void StartRename((ChatModel Chat, bool InHeader) rename) => _renaming.Start(rename.Chat, rename.InHeader);

    /// <summary>Enter keeps the new name, and Escape drops it.</summary>
    private Task RenameKey((KeyboardEventArgs Key, ChatModel Chat) e) => _renaming.KeyAsync(e.Key.Key, e.Chat);

    /// <summary>Keeps the new name and closes the field.</summary>
    private Task FinishRename(ChatModel chat) => _renaming.FinishAsync(chat);

    /// <summary>Pins the chat to the favourites, or takes it off.</summary>
    private async Task ToggleFavourite(ChatModel chat)
    {
        chat.Favourite = !chat.Favourite;
        await Store.SaveAsync();
    }

    /// <summary>Moves the chat out of the recent ones, or back.</summary>
    private async Task ToggleArchive(ChatModel chat)
    {
        chat.Archived = !chat.Archived;
        if (chat.Archived) chat.Favourite = false;
        await Store.SaveAsync();
    }

    /// <summary>Deletes the chat, here and on the API.</summary>
    private async Task DeleteChat(ChatModel chat)
    {
        if (await Runner.DeleteChatAsync(chat)) Nav.NavigateTo(ChatPath);
    }

    /// <summary>Keeps the API key the sidebar was given.</summary>
    private Task SaveApiKey(string? apiKey) => Runner.SaveApiKeyAsync(apiKey);
}

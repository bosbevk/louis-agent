using LouisAgent.Choices;
using LouisAgent.Puls.Content;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Puls.Pages;

/// <summary>The chat page's own state and handlers; its layout is in the Razor file, a turn's life in the runner.
/// </summary>
public partial class ChatPage : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private ChatStore Store { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private CurrencyState Currency { get; set; } = default!;
    [Inject] private ChatText Text { get; set; } = default!;

    [Parameter] public string? SessionId { get; set; }

    private ChatRunner Runner { get; set; } = default!;

    /// <summary>Puls speaks Afrikaans unless this browser chose another language the API offers.</summary>
    private const string DefaultLanguage = "af";

    private string _language = DefaultLanguage;
    private IReadOnlyList<string> _languages = [DefaultLanguage];
    private string? _model;
    private bool _searchOpen;
    private bool _renameInHeader;
    private string? _renaming;
    private string _renameDraft = "";
    private bool _focusRename;
    private bool _focusDraft;
    private string? _toast;

    // Where the user is (F22): this browser's setting, sent with a new chat only when set.
    private string? _location;
    private bool _locating;

    // Animations although the system asks for less (F24): this browser's choice.
    private bool _motion, _reducedMotion;

    /// <summary>
    /// Puls's mode for a new chat (F21, CL9): <c>puls/voice-{code}</c>, its voice in the chosen language, then Puls's
    /// shared routing (Puls's <c>Prompts/agent/puls/</c>). A running chat keeps the voice it opened with.
    /// </summary>
    private string Role => $"puls/voice-{_language}";

    /// <summary>Which view the shell shows (F20): the chat, Verken or Studio, from the address.</summary>
    private string View => Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0] switch
    {
        "verken" => "verken",
        "studio" => "studio",
        _ => "chat",
    };

    /// <summary>Sets up the runner, listens for changes, and loads what the page needs.</summary>
    protected override async Task OnInitializedAsync()
    {
        Runner = new ChatRunner(Api, Store, Text, OpenSessionAsync);
        Runner.Changed += Redraw;
        Runner.SessionOpened += id => Nav.NavigateTo($"gesels/{id}");
        Currency.Changed += Redraw;
        // /gesels, /verken and /studio are this page with the same (empty) parameters, so Blazor wouldn't redraw on its own.
        Nav.LocationChanged += OnLocationChanged;
        if (!Store.Loaded) await Store.LoadAsync();
        _location = await JS.InvokeAsync<string?>("chatUi.loadLocation");
        _motion = await JS.InvokeAsync<bool>("chatUi.motion");
        await LoadLanguagesAsync();
        _reducedMotion = await JS.InvokeAsync<bool>("chatUi.reducedMotion");
        try
        {
            var model = await Api.GetAsync<System.Text.Json.JsonElement>("v1/model");
            _model = Pretty(model.GetProperty("model").GetString());
        }
        catch (Exception)
        {
            _model = null; // An older API without /v1/model: the picker says "Model".
        }
    }

    /// <summary>
    /// A new chat's session: Puls answers the web itself and hands cooking to the cook (F29; before it, F5: agents
    /// only, every task to a specialist). An API without a peer offers no specialists toolset; the chat then gets every
    /// toolset there is. The time zone always (GetToday in the user's own time; it names a region, not a place); the
    /// location only when set.
    /// </summary>
    private async Task<string> OpenSessionAsync()
    {
        string[] available = await Api.AvailableToolsetsAsync();
        string[] own = [AskedToolset.Specialists.Wire(), AskedToolset.Web.Wire()];
        string? timeZone = await JS.InvokeAsync<string?>("chatUi.timeZone");
        return await Api.CreateSessionAsync(
            available.Contains(AskedToolset.Specialists.Wire()) ? own.Where(available.Contains).ToArray() : available,
            Role, _location, timeZone);
    }

    /// <summary>The languages Puls can speak: a voice for each in the API's roles, so the two agree.</summary>
    private async Task LoadLanguagesAsync()
    {
        try
        {
            var roles = await Api.GetAsync<System.Text.Json.JsonElement>("v1/roles");
            var codes = roles.GetProperty("agent").EnumerateArray().Select(p => p.GetString() ?? "")
                .Where(p => p.StartsWith("puls/voice-", StringComparison.Ordinal))
                .Select(p => p["puls/voice-".Length..]).ToList();
            if (codes.Count > 0) _languages = codes;
        }
        catch (Exception)
        {
            // An API without /v1/roles or its levels: Afrikaans, as before CL9.
        }

        string? saved = await JS.InvokeAsync<string?>("chatUi.loadLanguage");
        _language = saved is not null && _languages.Contains(saved) ? saved :
            _languages.Contains(DefaultLanguage) ? DefaultLanguage : _languages[0];
    }

    /// <summary>Opens the chat the address names; an old <c>/chat/{id}</c> link lands at its new address.</summary>
    protected override void OnParametersSet()
    {
        // The chat moved to /gesels (F20); an old /chat/{id} link lands on the same chat at its new address.
        if (SessionId is not null && Nav.ToBaseRelativePath(Nav.Uri).StartsWith("chat/", StringComparison.Ordinal))
        {
            Nav.NavigateTo($"gesels/{SessionId}", replace: true);
            return;
        }

        var chat = Store.Find(SessionId);
        Runner.Chat = chat;
        if (SessionId is not null && chat is null && !Runner.Streaming)
            Runner.Notice = "Daardie gesprek is nie in hierdie blaaier gestoor nie.";
    }

    /// <summary>Does what had to wait for the draw, such as putting the cursor in the message box.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusDraft)
        {
            _focusDraft = false;
            await JS.InvokeVoidAsync("chatUi.focusEnd", "#composer-input");
        }

        if (_focusRename)
        {
            _focusRename = false;
            await JS.InvokeVoidAsync("chatUi.focus", "#rename-input", true);
        }

        // A cost that changed since the last render counts up to its new value (F24; data-count elements, js/chat.js).
        await JS.InvokeVoidAsync("chatUi.countCosts");
    }

    /// <summary>Something the page shows changed, so draw again.</summary>
    private void Redraw() => InvokeAsync(StateHasChanged);

    /// <summary>The address changed, so draw again.</summary>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(StateHasChanged);

    /// <summary>Stops listening when the page goes.</summary>
    public void Dispose()
    {
        Runner.Changed -= Redraw;
        Currency.Changed -= Redraw;
        Nav.LocationChanged -= OnLocationChanged;
    }

    /// <summary>Goes back to an empty chat.</summary>
    private void NewChat()
    {
        Runner.Notice = null;
        Nav.NavigateTo("gesels");
    }

    /// <summary>Sends a suggested question, unless a turn is already running.</summary>
    private async Task Suggest(string prompt)
    {
        if (Runner.Streaming) return;
        await Runner.StartTurnAsync(prompt, []);
    }

    /// <summary>A template's card: a new chat with its starter in the message box.</summary>
    private async Task StartTemplate(Template template)
    {
        Nav.NavigateTo("gesels");
        await SetDraft(template.Starter);
    }

    /// <summary>Puts <paramref name="text"/> in the message box, for the user to finish and send.</summary>
    private Task SetDraft(string text)
    {
        Runner.Draft = text;
        // Focused and grown once the box shows the text (after the render, possibly on another view's page).
        _focusDraft = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Kept in this browser; the next chat opens in it, and the open one keeps its voice.</summary>
    private async Task ChooseLanguage(string code)
    {
        _language = code;
        await JS.InvokeVoidAsync("chatUi.saveLanguage", code);
    }

    /// <summary>Copies the text and says so.</summary>
    private async Task Copy(string text)
    {
        bool copied = await JS.InvokeAsync<bool>("chatUi.copy", text);
        _ = ShowToast(copied ? "Gekopieer." : "Kon nie kopieer nie.");
    }

    /// <summary>A chat's whole cost: Puls's own requests and its specialists', which its usage lacks.</summary>
    private static TurnUsage? ChatTotal(ChatModel chat) =>
        chat.Turns.SelectMany(t => t.Blocks.Select(b => b.SubTurn?.Usage).Prepend(t.Usage)).OfType<TurnUsage>()
            .Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u);

    /// <summary>The chats used this calendar month, in this browser.</summary>
    private List<ChatModel> MonthChats =>
        Store.Chats.Where(c =>
            c.UpdatedAt.LocalDateTime.Year == DateTime.Now.Year &&
            c.UpdatedAt.LocalDateTime.Month == DateTime.Now.Month).ToList();

    private string MonthCost => Money(MonthChats.Select(ChatTotal).OfType<TurnUsage>()
        .Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u));

    private string ChatCost => Runner.Chat is null ? "—" : Money(ChatTotal(Runner.Chat));

    /// <summary>A usage record's cost in the chosen currency; none counts as nothing spent.</summary>
    private string Money(TurnUsage? usage) => usage is null
        ? UsageFormat.Money(0, null, Currency.Current)
        : UsageFormat.Cost(usage, Currency.Current);

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
    private async Task SaveLocation(string? value)
    {
        value = value?.Trim();
        _location = string.IsNullOrEmpty(value) ? null : value;
        await JS.InvokeVoidAsync("chatUi.saveLocation", _location);
    }

    /// <summary>Keeps whether the page may move.</summary>
    private async Task SaveMotion(bool motion)
    {
        _motion = motion;
        await JS.InvokeVoidAsync("chatUi.setMotion", _motion);
    }

    /// <summary>Asks the browser where we are and keeps the place, or says why it can't.</summary>
    private async Task UseMyLocation()
    {
        _locating = true;
        try
        {
            var found = await JS.InvokeAsync<LocateResult>("chatUi.locate");
            if (found.Place is { Length: > 0 } place)
            {
                _location = place;
                await JS.InvokeVoidAsync("chatUi.saveLocation", place);
                _ = ShowToast($"Ligging: {place}");
            }
            else
            {
                _ = ShowToast(found.Error ?? "Kon nie jou ligging kry nie.");
            }
        }
        finally
        {
            _locating = false;
        }
    }

    /// <summary>What the page's script answers about the location.</summary>
    private sealed record LocateResult(string? Place, string? Error);

    /// <summary>Shows a short message, then takes it away.</summary>
    private async Task ShowToast(string text)
    {
        _toast = text;
        StateHasChanged();
        await Task.Delay(2800);
        if (_toast == text)
        {
            _toast = null;
            StateHasChanged();
        }
    }

    /// <summary>A placeholder's answer: a toast that says what's coming, gone after a few seconds.</summary>
    private async Task ComingSoon(string what)
    {
        string toast = $"{what}: kom binnekort.";
        _toast = toast;
        StateHasChanged();
        await Task.Delay(2800);
        if (_toast == toast) _toast = null;
    }

    /// <summary>Opens the rename field, in the sidebar or the header.</summary>
    private void StartRename((ChatModel Chat, bool InHeader) rename)
    {
        _renameInHeader = rename.InHeader;
        _renaming = rename.Chat.SessionId;
        _renameDraft = rename.Chat.Title;
        _focusRename = true;
    }

    /// <summary>Enter keeps the new name, and Escape drops it.</summary>
    private async Task RenameKey((KeyboardEventArgs Key, ChatModel Chat) e)
    {
        if (e.Key.Key == "Enter") await FinishRename(e.Chat);
        else if (e.Key.Key == "Escape") _renaming = null;
    }

    /// <summary>Keeps the new name and closes the field.</summary>
    private async Task FinishRename(ChatModel chat)
    {
        if (_renaming != chat.SessionId) return;
        _renaming = null;
        string title = _renameDraft.Trim();
        if (title.Length == 0 || title == chat.Title) return;
        chat.Title = title.Length > 80 ? title[..80] : title;
        await Store.SaveAsync();
    }

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
        if (await Runner.DeleteChatAsync(chat)) Nav.NavigateTo("gesels");
    }

    /// <summary>Keeps the API key the sidebar was given.</summary>
    private Task SaveApiKey(string? apiKey) => Runner.SaveApiKeyAsync(apiKey);
}

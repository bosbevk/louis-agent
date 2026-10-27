using LouisAgent.Puls.Content;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Pages;

/// <summary>The chat page's own state and handlers (W2); the layout is in ChatPage.razor, a turn's life in <see cref="ChatRunner"/>.</summary>
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
    /// shared routing (louis-agent.core/Prompts/agent/puls/). A running chat keeps the voice it opened with.
    /// </summary>
    private string Role => $"puls/voice-{_language}";

    /// <summary>Which view the shell shows (F20): the chat, Verken or Studio, from the address.</summary>
    private string View => Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0] switch
    {
        "verken" => "verken",
        "studio" => "studio",
        _ => "chat",
    };

    protected override async Task OnInitializedAsync()
    {
        Runner = new ChatRunner(Api, Store, Text, OpenSessionAsync);
        Runner.Changed += Redraw;
        Runner.SessionOpened += id => Nav.NavigateTo($"gesels/{id}");
        Currency.Changed += Redraw;
        // /gesels, /verken and /studio are this page with the same (empty) parameters, so Blazor wouldn't redraw on its own.
        Nav.LocationChanged += OnLocationChanged;
        if (!Store.Loaded) await Store.LoadAsync();
        _location = await JS.InvokeAsync<string?>("pulsUi.loadLocation");
        _motion = await JS.InvokeAsync<bool>("pulsUi.motion");
        await LoadLanguagesAsync();
        _reducedMotion = await JS.InvokeAsync<bool>("pulsUi.reducedMotion");
        try
        {
            var model = await Api.GetAsync<System.Text.Json.JsonElement>("v1/model");
            _model = Pretty(model.GetProperty("model").GetString());
        }
        catch (Exception)
        {
            _model = null;   // An older API without /v1/model: the picker says "Model".
        }
    }

    /// <summary>
    /// A new chat's session: Puls answers the web itself and hands cooking to the cook (F29; before it, F5: agents only,
    /// every task to a specialist). An API without a peer offers no specialists toolset; the chat then gets every
    /// toolset there is. The time zone always (GetToday in the user's own time; it names a region, not a place); the
    /// location only when set.
    /// </summary>
    private async Task<string> OpenSessionAsync()
    {
        string[] available = await Api.AvailableToolsetsAsync();
        string[] own = [AskedToolset.Specialists.Name(), AskedToolset.Web.Name()];
        string? timeZone = await JS.InvokeAsync<string?>("pulsUi.timeZone");
        return await Api.CreateSessionAsync(available.Contains(AskedToolset.Specialists.Name()) ? own.Where(available.Contains).ToArray() : available, Role, _location, timeZone);
    }

    /// <summary>The languages Puls can speak: a puls/voice-{code} for each in GET /v1/roles, so the menu and the API agree.</summary>
    private async Task LoadLanguagesAsync()
    {
        try
        {
            var roles = await Api.GetAsync<System.Text.Json.JsonElement>("v1/roles");
            var codes = roles.GetProperty("agent").EnumerateArray().Select(p => p.GetString() ?? "")
                .Where(p => p.StartsWith("puls/voice-", StringComparison.Ordinal)).Select(p => p["puls/voice-".Length..]).ToList();
            if (codes.Count > 0) _languages = codes;
        }
        catch (Exception)
        {
            // An API without /v1/roles or its levels: Afrikaans, as before CL9.
        }
        string? saved = await JS.InvokeAsync<string?>("pulsUi.loadLanguage");
        _language = saved is not null && _languages.Contains(saved) ? saved : _languages.Contains(DefaultLanguage) ? DefaultLanguage : _languages[0];
    }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusDraft)
        {
            _focusDraft = false;
            await JS.InvokeVoidAsync("pulsUi.focusEnd", "#composer-input");
        }
        if (_focusRename)
        {
            _focusRename = false;
            await JS.InvokeVoidAsync("pulsUi.focus", "#rename-input", true);
        }
        // A cost that changed since the last render counts up to its new value (F24; data-count elements, js/chat.js).
        await JS.InvokeVoidAsync("pulsUi.countCosts");
    }

    private void Redraw() => InvokeAsync(StateHasChanged);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Runner.Changed -= Redraw;
        Currency.Changed -= Redraw;
        Nav.LocationChanged -= OnLocationChanged;
    }

    private void NewChat()
    {
        Runner.Notice = null;
        Nav.NavigateTo("gesels");
    }

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

    /// <summary>Kept in this browser; the next chat opens in it, the open one keeps its voice (its prompt stays cached).</summary>
    private async Task ChooseLanguage(string code)
    {
        _language = code;
        await JS.InvokeVoidAsync("pulsUi.saveLanguage", code);
    }

    private async Task Copy(string text)
    {
        bool copied = await JS.InvokeAsync<bool>("pulsUi.copy", text);
        _ = ShowToast(copied ? "Gekopieer." : "Kon nie kopieer nie.");
    }

    /// <summary>A chat's whole cost: Puls's own requests and its specialists' (the chat's usage doesn't hold them yet: F5 roll-up).</summary>
    private static TurnUsage? ChatTotal(ChatModel chat) =>
        chat.Turns.SelectMany(t => t.Blocks.Select(b => b.SubTurn?.Usage).Prepend(t.Usage)).OfType<TurnUsage>()
            .Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u);

    /// <summary>The chats used this calendar month, in this browser.</summary>
    private List<ChatModel> MonthChats =>
        Store.Chats.Where(c => c.UpdatedAt.LocalDateTime.Year == DateTime.Now.Year && c.UpdatedAt.LocalDateTime.Month == DateTime.Now.Month).ToList();

    private string MonthCost => Money(MonthChats.Select(ChatTotal).OfType<TurnUsage>().Aggregate((TurnUsage?)null, (sum, u) => sum is null ? u : sum + u));

    private string ChatCost => Runner.Chat is null ? "—" : Money(ChatTotal(Runner.Chat));

    private string Money(TurnUsage? usage) => usage is null ? UsageFormat.Money(0, null, Currency.Current) : UsageFormat.Cost(usage, Currency.Current);

    private static string? Pretty(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var words = id.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var named = words.TakeWhile(w => !char.IsDigit(w[0])).Select(w => char.ToUpperInvariant(w[0]) + w[1..]);
        string version = string.Join(".", words.SkipWhile(w => !char.IsDigit(w[0])));
        return string.Join(" ", named) + (version.Length > 0 ? " " + version : "");
    }

    private async Task SaveLocation(string? value)
    {
        value = value?.Trim();
        _location = string.IsNullOrEmpty(value) ? null : value;
        await JS.InvokeVoidAsync("pulsUi.saveLocation", _location);
    }

    private async Task SaveMotion(bool motion)
    {
        _motion = motion;
        await JS.InvokeVoidAsync("pulsUi.setMotion", _motion);
    }

    private async Task UseMyLocation()
    {
        _locating = true;
        try
        {
            var found = await JS.InvokeAsync<LocateResult>("pulsUi.locate");
            if (found.Place is { Length: > 0 } place)
            {
                _location = place;
                await JS.InvokeVoidAsync("pulsUi.saveLocation", place);
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

    private sealed record LocateResult(string? Place, string? Error);

    private async Task ShowToast(string text)
    {
        _toast = text;
        StateHasChanged();
        await Task.Delay(2800);
        if (_toast == text) { _toast = null; StateHasChanged(); }
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

    private void StartRename((ChatModel Chat, bool InHeader) rename)
    {
        _renameInHeader = rename.InHeader;
        _renaming = rename.Chat.SessionId;
        _renameDraft = rename.Chat.Title;
        _focusRename = true;
    }

    private async Task RenameKey((KeyboardEventArgs Key, ChatModel Chat) e)
    {
        if (e.Key.Key == "Enter") await FinishRename(e.Chat);
        else if (e.Key.Key == "Escape") _renaming = null;
    }

    private async Task FinishRename(ChatModel chat)
    {
        if (_renaming != chat.SessionId) return;
        _renaming = null;
        string title = _renameDraft.Trim();
        if (title.Length == 0 || title == chat.Title) return;
        chat.Title = title.Length > 80 ? title[..80] : title;
        await Store.SaveAsync();
    }

    private async Task ToggleFavourite(ChatModel chat)
    {
        chat.Favourite = !chat.Favourite;
        await Store.SaveAsync();
    }

    private async Task ToggleArchive(ChatModel chat)
    {
        chat.Archived = !chat.Archived;
        if (chat.Archived) chat.Favourite = false;
        await Store.SaveAsync();
    }

    private async Task DeleteChat(ChatModel chat)
    {
        if (await Runner.DeleteChatAsync(chat)) Nav.NavigateTo("gesels");
    }

    private Task SaveApiKey(string? apiKey) => Runner.SaveApiKeyAsync(apiKey);
}

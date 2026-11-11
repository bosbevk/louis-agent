using LouisAgent.Choices;
using LouisAgent.Puls.Content;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace LouisAgent.Puls.Pages;

/// <summary>What only Puls knows about its chat page: Verken and Studio, the old address, and which toolsets it asks
/// for. The frame is the framework's.</summary>
public partial class ChatPage : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = default!;
    [Inject] private ChatStore Store { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private CurrencyState Currency { get; set; } = default!;

    [Parameter] public string? SessionId { get; set; }

    private ChatShell _shell = null!;

    /// <summary>The header's title for a view that isn't the chat.</summary>
    private string? ViewTitle => View switch
    {
        "verken" => "Verken",
        "studio" => "Studio",
        _ => null,
    };

    /// <summary>Which view the page shows: the chat, Verken or Studio, from the address.</summary>
    private string View => Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0] switch
    {
        "verken" => "verken",
        "studio" => "studio",
        _ => "chat",
    };

    /// <summary>Listens for the address.</summary>
    protected override void OnInitialized()
    {
        // /gesels, /verken and /studio are this page with the same (empty) parameters, so Blazor wouldn't redraw on its own.
        Nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>An old <c>/chat/{id}</c> link lands on the same chat at its new address.</summary>
    protected override void OnParametersSet()
    {
        if (SessionId is not null && Nav.ToBaseRelativePath(Nav.Uri).StartsWith("chat/", StringComparison.Ordinal))
            Nav.NavigateTo($"gesels/{SessionId}", replace: true);
    }

    /// <summary>
    /// A new chat's session: Puls answers the web itself and asks the cook about cooking. An API without a peer offers
    /// no agents toolset; the chat then gets every toolset there is. The time zone always (GetToday in the user's own
    /// time; it names a region, not a place); the location only when set.
    /// </summary>
    private async Task<string> OpenSessionAsync(ChatOpening opening)
    {
        string[] available = await Api.AvailableToolsetsAsync();
        string[] own = [AskedToolset.Agents.Wire(), AskedToolset.Web.Wire()];
        return await Api.CreateSessionAsync(
            available.Contains(AskedToolset.Agents.Wire()) ? own.Where(available.Contains).ToArray() : available,
            opening.Role, opening.Location, opening.TimeZone);
    }

    /// <summary>A template's card: a new chat with its starter in the message box.</summary>
    private async Task StartTemplate(Template template)
    {
        Nav.NavigateTo("gesels");
        await _shell.SetDraftAsync(template.Starter);
    }

    /// <summary>The address changed, so draw again.</summary>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(StateHasChanged);

    /// <summary>Stops listening when the page goes.</summary>
    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}

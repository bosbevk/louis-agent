using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// The two buttons a chat window has (F127.2): the first folds it to its head, the second spreads it wide, and each
/// brings it back while its own size is on. The guide's widget and a mock website's chat both wear them.
/// </summary>
/// <remarks>
/// State, kept by the window: the buttons only ask for a size through <see cref="SizeChanged"/>, so a window binds its
/// own <see cref="ChatWindowSize"/> with <c>@bind-Size</c> and draws itself from it, and the one-size-at-a-time rule
/// lives here once instead of in each window. The words are the chat's (<see cref="ChatText"/>), so a site says them in
/// its own language.
/// </remarks>
public partial class WindowButtons
{
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The size the window is in.</summary>
    [Parameter]
    public ChatWindowSize Size { get; set; }

    /// <summary>Asked for the size a button wants.</summary>
    [Parameter]
    public EventCallback<ChatWindowSize> SizeChanged { get; set; }

    /// <summary>The site's words, or the English ones.</summary>
    private ChatText Text => Services.GetService<ChatText>() ?? new ChatText();

    /// <summary>The first button folds the window, or brings a folded one back.</summary>
    private string MinimiseLabel => Size == ChatWindowSize.Minimised ? Text.Restore : Text.Minimise;

    /// <summary>The second button spreads the window wide, or brings a spread one back.</summary>
    private string MaximiseLabel => Size == ChatWindowSize.Maximised ? Text.Restore : Text.Maximise;

    /// <summary>Whether a button's size is the one the window is in, for a screen reader.</summary>
    private string Pressed(ChatWindowSize size) => Size == size ? "true" : "false";

    /// <summary>Asks to fold the window, or to bring a folded one back.</summary>
    private Task ToggleMinimisedAsync() => SizeChanged.InvokeAsync(
        Size == ChatWindowSize.Minimised ? ChatWindowSize.Normal : ChatWindowSize.Minimised);

    /// <summary>Asks to spread the window wide, or to bring a spread one back.</summary>
    private Task ToggleMaximisedAsync() => SizeChanged.InvokeAsync(
        Size == ChatWindowSize.Maximised ? ChatWindowSize.Normal : ChatWindowSize.Maximised);
}

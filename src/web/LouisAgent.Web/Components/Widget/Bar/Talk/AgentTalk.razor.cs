using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Widget.Bar.Talk;

/// <summary>
/// The whole talk over the agent's bar (F134.4): every turn of the chat, with what it has cost at its foot. The bar
/// shows it when the visitor asks for it, or when an answer holds what only the talk draws, such as a form.
/// </summary>
/// <remarks>
/// It draws the chat it is given through the chat's own list, so a turn looks here as it does in any chat, and it
/// hands a choice or its close back up: the bar decides what stands over it.
/// </remarks>
public partial class AgentTalk
{
    /// <summary>The chat whose turns it shows.</summary>
    [Parameter, EditorRequired]
    public ChatModel? Chat { get; set; }

    /// <summary>Whether the last turn is still coming in.</summary>
    [Parameter]
    public bool Streaming { get; set; }

    /// <summary>The currency the talk's numbers are shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Whether the talk shows the agent's work, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>Whether a card's answer shows in the talk, as the widget's panel does.</summary>
    [Parameter]
    public bool ShowCardAnswers { get; set; }

    /// <summary>Answers with a choice the agent offered, or a form it asked with.</summary>
    [Parameter]
    public EventCallback<string> Chosen { get; set; }

    /// <summary>Asks to fold the talk away.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;
}

using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// The chat widget's bar (F134.3): the guide at the foot of the page, with the box always there to type in.
/// </summary>
/// <remarks>
/// A view of the widget's own runner, not a second chat: it reads the runner and hands a send back to the widget, so
/// the session, the door, the page that travels with a message and the tool calls stay in one place (F134's rule 4,
/// one engine with a layout).
/// </remarks>
public partial class AgentBar
{
    /// <summary>The widget's runner: the chat, the draft and whether it is streaming.</summary>
    [Parameter, EditorRequired]
    public ChatRunner Runner { get; set; } = null!;

    /// <summary>What the empty box says.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Asks the widget to send what is in the box.</summary>
    [Parameter]
    public EventCallback OnSend { get; set; }

    /// <summary>The chat's words, for the composer's buttons.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;
}

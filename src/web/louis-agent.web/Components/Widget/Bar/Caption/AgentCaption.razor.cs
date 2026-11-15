using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// The agent's caption (F134.4): its last answer, standing beside the place it showed, with its name, its words and a
/// way into the box. The page's choices, when the agent offered some, stand in it too.
/// </summary>
/// <remarks>
/// It draws what it is given and moves nothing: the agent's own call to show a place is what lit the place (F134's
/// rule 2), and the bar decides when an answer stands as a caption.
/// </remarks>
public partial class AgentCaption
{
    /// <summary>Who is speaking: the agent's name.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>What it said, as markdown.</summary>
    [Parameter, EditorRequired]
    public string Words { get; set; } = "";

    /// <summary>What stands under its words, such as the choices it offered.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Asks to see the whole talk instead.</summary>
    [Parameter]
    public EventCallback OnTalk { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;
}

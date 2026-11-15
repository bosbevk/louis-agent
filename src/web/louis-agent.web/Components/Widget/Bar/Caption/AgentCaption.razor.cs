using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// The agent's caption (F134.4): its last answer, standing beside the place it showed, with its name, its words and a
/// way into the box. The choices it offered, when it offered some, stand in it too.
/// </summary>
/// <remarks>
/// It draws what it is given and moves nothing: the agent's own call to show a place is what lit the place (F134's
/// rule 2), and <see cref="Captioned"/> decides when an answer stands as a caption. Its choices are the talk's own
/// cards, so a choice looks and answers the same wherever it is drawn.
/// </remarks>
public partial class AgentCaption
{
    /// <summary>Who is speaking: the agent's name.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>What it said, as markdown.</summary>
    [Parameter, EditorRequired]
    public string Words { get; set; } = "";

    /// <summary>The choices it offered with them; none when it offered none.</summary>
    [Parameter]
    public Offered? Offer { get; set; }

    /// <summary>Asks with a choice's label, as a card in the talk does.</summary>
    [Parameter]
    public EventCallback<string> OnChoose { get; set; }

    /// <summary>Asks to see the whole talk instead.</summary>
    [Parameter]
    public EventCallback OnTalk { get; set; }

    /// <summary>The chat's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;
}

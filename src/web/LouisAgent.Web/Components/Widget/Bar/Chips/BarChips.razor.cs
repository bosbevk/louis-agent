using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Bar.Chips;

/// <summary>
/// The chips over the agent bar while the guide waits (F134.3): the questions to ask, one drawn bright, and a note that
/// the box is there too. While the bar is closed they stand round its orb in two rows (FX173).
/// </summary>
/// <remarks>
/// A part of its own, split from the bar when the bar's styles outgrew their file (FX173): the bar decides what the
/// chips are and whether they show, and this draws them and holds their styles.
/// </remarks>
public partial class BarChips
{
    /// <summary>The chips to draw, the site's questions or the guide's last offer.</summary>
    [Parameter, EditorRequired]
    public AgentChips? Chips { get; set; }

    /// <summary>Whether they show, or stand stepped away and out of reach.</summary>
    [Parameter]
    public bool Show { get; set; }

    /// <summary>Asks the chip's question.</summary>
    [Parameter]
    public EventCallback<string> OnChoose { get; set; }

    /// <summary>The chat's words, for the note.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    /// <summary>The chips' classes: showing, or stepped away.</summary>
    private string Classes => Show ? "agent-chips" : "agent-chips is-away";
}

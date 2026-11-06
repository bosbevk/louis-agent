using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Sidebar;

/// <summary>Animations although the system asks for less: this browser's choice.</summary>
public partial class MotionToggle
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter] public bool Motion { get; set; }

    /// <summary>Whether the system asks for less motion; without that the choice changes nothing, and the label says so.</summary>
    [Parameter] public bool ReducedMotion { get; set; }
    [Parameter] public EventCallback<bool> OnChange { get; set; }
}

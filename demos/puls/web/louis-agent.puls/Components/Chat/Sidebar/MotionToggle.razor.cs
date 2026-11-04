using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Sidebar;

public partial class MotionToggle
{
    [Parameter] public bool Motion { get; set; }

    /// <summary>Whether the system asks for less motion; without that the choice changes nothing, and the label says so.</summary>
    [Parameter] public bool ReducedMotion { get; set; }
    [Parameter] public EventCallback<bool> OnChange { get; set; }
}

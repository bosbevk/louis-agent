using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Sidebar;

public partial class LocationPicker
{
    [Parameter] public string? Location { get; set; }
    [Parameter] public bool Locating { get; set; }

    /// <summary>The typed place, as typed; the page trims it and keeps it in this browser.</summary>
    [Parameter] public EventCallback<string?> OnSave { get; set; }
    [Parameter] public EventCallback OnLocate { get; set; }
}

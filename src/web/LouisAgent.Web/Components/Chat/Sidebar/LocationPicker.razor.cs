using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Sidebar;

/// <summary>Where the user is: typed, or found by the browser.</summary>
public partial class LocationPicker
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter] public string? Location { get; set; }
    [Parameter] public bool Locating { get; set; }

    /// <summary>The typed place, as typed; the page trims it and keeps it in this browser.</summary>
    [Parameter] public EventCallback<string?> OnSave { get; set; }
    [Parameter] public EventCallback OnLocate { get; set; }
}

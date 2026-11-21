using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat;

/// <summary>The Send and Stop buttons, whichever fits the moment.</summary>
public partial class ComposerButtons
{
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public bool CanSend { get; set; }
    [Parameter] public EventCallback OnSend { get; set; }
    [Parameter] public EventCallback OnStop { get; set; }
    [Parameter] public string SendLabel { get; set; } = "Send";
    [Parameter] public string StopLabel { get; set; } = "Stop";
    [Parameter] public string? SendTitle { get; set; }
    [Parameter] public string? StopTitle { get; set; }
}

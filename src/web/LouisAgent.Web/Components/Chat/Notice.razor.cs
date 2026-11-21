using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat;

/// <summary>A closable line that tells the person something.</summary>
public partial class Notice
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}

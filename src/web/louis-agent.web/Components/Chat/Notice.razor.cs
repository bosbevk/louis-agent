using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat;

public partial class Notice
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}

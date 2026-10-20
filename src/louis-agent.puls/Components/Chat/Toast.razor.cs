using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat;

public partial class Toast
{
    [Parameter] public string? Text { get; set; }
}

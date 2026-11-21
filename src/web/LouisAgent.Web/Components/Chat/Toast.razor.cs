using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat;

/// <summary>A short word at the bottom of the page, such as that something was copied.</summary>
public partial class Toast
{
    /// <summary>What it says; with none, it doesn't show.</summary>
    [Parameter] public string? Text { get; set; }
}

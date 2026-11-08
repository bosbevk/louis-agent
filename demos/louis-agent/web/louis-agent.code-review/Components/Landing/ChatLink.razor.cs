using LouisAgent.CodeReview.Content;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components.Landing;

/// <summary>
/// A link to the coding agent's chat. The site used to be a page apart from the app and read where the chat lived
/// from a setting; now the chat is this app's own page, so the link knows the address (F109.1). While
/// <see cref="SiteDoors.CodingAgent"/> is closed it says "Coming soon" and leads nowhere (FX88).
/// </summary>
public partial class ChatLink
{
    /// <summary>The link's look.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>What the link says.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the chat is: this app's chat page.</summary>
    [Parameter]
    public string Address { get; set; } = "/chat";
}

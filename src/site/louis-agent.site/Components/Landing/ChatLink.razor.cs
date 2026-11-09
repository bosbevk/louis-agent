using LouisAgent.Site.Content;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Landing;

/// <summary>
/// A way to the coding agent. The site used to be a page apart from the app and read where the chat lived from a
/// setting; now the chat is this app's own page, so the link knows the address (F109.1). While
/// <see cref="SiteDoors.CodingAgent"/> is closed it leads to the recorded session instead, in its own words (F120).
/// </summary>
public partial class ChatLink
{
    /// <summary>Where the recorded session is: the coding agent's page.</summary>
    private const string Recording = "/coding-agent";

    /// <summary>The link's look.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>What the link says when it opens the chat.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the chat is: this app's chat page.</summary>
    [Parameter]
    public string Address { get; set; } = "/chat";

    /// <summary>What the link says while the chat is closed and it leads to the recording.</summary>
    [Parameter]
    public string Watch { get; set; } = "";
}

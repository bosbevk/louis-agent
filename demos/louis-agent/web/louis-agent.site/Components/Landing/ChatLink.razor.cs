using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace LouisAgent.Site.Components.Landing;

/// <summary>
/// A link to the coding agent's chat. This site is its landing page and references no engine project, so all it knows
/// of the app is where it lives: <c>/chat</c>, unless <c>CHAT_ADDRESS</c> says it runs somewhere else.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class ChatLink
{
    [Inject] private IConfiguration Settings { get; set; } = null!;

    /// <summary>The link's look.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>What the link says.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the chat is.</summary>
    private string Address => Settings["CHAT_ADDRESS"] ?? "/chat";
}

using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Starter.Pages;

/// <summary>
/// What only this site knows about its chat: which toolsets it asks for, and what it turns off. It starts with what needs
/// nothing more set up; turn a part back on when your site has what it needs.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class ChatPage
{
    /// <summary>What needs more than a new site has yet, or only says it's coming.</summary>
    private static readonly ChatFeatures Features =
        new(Credits: false, Language: false, Location: false, Projects: false, Unfinished: false);

    private ChatShell _shell = null!;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatHandoff Handoff { get; set; } = null!;

    /// <summary>The chat the address names; null for a new one.</summary>
    [Parameter] public string? SessionId { get; set; }

    /// <summary>A prompt from an admin screen waits in the box, for the person to read and send.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Handoff.Take() is { } draft)
            await _shell.SetDraftAsync(draft);
    }

    /// <summary>A new chat's session: every toolset the API can load, and the person's time zone.</summary>
    private async Task<string> OpenSessionAsync(ChatOpening opening) =>
        await Api.CreateSessionAsync(await Api.AvailableToolsetsAsync(), timeZone: opening.TimeZone);
}

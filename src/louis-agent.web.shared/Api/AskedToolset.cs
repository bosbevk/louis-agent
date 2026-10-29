namespace LouisAgent.Web.Shared.Api;

/// <summary>
/// The toolsets the web apps ask the API for when they open a chat (ADR 0043). The browser can't reference core, so this is the browser side's own copy of the names; a test in
/// <c>api.tests</c> checks each member against core's <c>CoreToolset</c>, so the two cannot drift. A member's lower-case name is its wire name.
/// </summary>
public enum AskedToolset
{
    Specialists,
    Web,
}

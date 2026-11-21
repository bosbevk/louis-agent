using LouisAgent.Choices;

namespace LouisAgent.Web.Evals.Admin.Explorer.Place;

/// <summary>
/// The explorer's views, from the run down to one request; the wire name is what the address carries in
/// <c>?view=</c>, and the order is the depth (the run is level 0, a request level 4).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ExplorerView
{
    [WireName("run")] Run,
    [WireName("conversations")] Conversations,
    [WireName("conversation")] Conversation,
    [WireName("turn")] Turn,
    [WireName("request")] Request,
}

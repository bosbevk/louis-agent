using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Chats.Calls;

/// <summary>
/// One choice offered as a card: the label the visitor clicks and sends, a line of why under it, whether the agent
/// suggests it, and (FX147) the kind of box it holds when the choice is the visitor's own words. The web's own reading
/// of the site pack's choice, which it can't reference (ADR 0027).
/// </summary>
public sealed record OfferChoice(string Label, string? Why = null, bool Suggested = false, AskedKind? Writes = null);

using LouisAgent.Packs.Site.Tools.Forms;

namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// One choice the agent offers as a card: the short label the visitor clicks (and sends as their message), an
/// optional one-line reason under it, whether it is the one the agent suggests, and (FX147) the kind of box it holds
/// when the choice is the visitor's own words, which they write and send from the card.
/// </summary>
public sealed record OfferChoice(string Label, string? Why = null, bool Suggested = false, FieldKind? Writes = null);

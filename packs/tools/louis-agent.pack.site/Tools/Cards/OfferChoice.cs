namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// One choice the agent offers as a card: the short label the visitor clicks (and sends as their message), an
/// optional one-line reason under it, and whether it is the one the agent suggests.
/// </summary>
public sealed record OfferChoice(string Label, string? Why = null, bool Suggested = false);

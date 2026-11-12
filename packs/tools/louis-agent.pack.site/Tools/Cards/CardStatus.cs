namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>Where a card stands, as the model writes it: its words, and a tone by name ("green" once sent).</summary>
public sealed record CardStatus(string Label, string? Tone = null);

namespace LouisAgent.Packs.Site.Messages.Holes.Cards;

/// <summary>Which part of a receipt runs long, the most it may be, and how long it is.</summary>
public sealed record ReceiptTooLongHoles(string Part, int Most, int Length);

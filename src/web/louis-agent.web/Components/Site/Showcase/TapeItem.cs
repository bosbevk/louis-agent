namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>One item on the running tape: its words, and a part after them in the accent, such as an address.</summary>
public sealed record TapeItem(string Text, string? Mark = null);

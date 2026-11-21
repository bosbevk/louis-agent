namespace LouisAgent.Composition.Packs.Screens;

/// <summary>One column of a screen: its heading, and what kind of value its cells hold.</summary>
public sealed record ScreenColumn(string Title, ScreenCell Kind);

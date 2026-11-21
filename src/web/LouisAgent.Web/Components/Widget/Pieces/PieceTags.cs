namespace LouisAgent.Web.Components.Widget.Pieces;

/// <summary>
/// The words of the small tag a piece of the page wears (FX163, the design's states): what it offers under the
/// pointer, what it says once you picked it, and what it says while the agent shows it.
/// </summary>
public sealed record PieceTags(string Ask, string Picked, string Shown);

namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>The part of the page shown, its length, and the tool that reads on.</summary>
public sealed record PageContinuesHoles(int Start, int End, int Length, string Tool);

namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>Where the reader asked to start, and how long the text is.</summary>
public sealed record FetchStartPastEndHoles(int StartIndex, int Length);

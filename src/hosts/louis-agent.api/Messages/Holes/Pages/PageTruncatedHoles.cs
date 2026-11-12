namespace LouisAgent.Api.Messages.Holes.Pages;

/// <summary>How much of a long page went, and how long it was.</summary>
public sealed record PageTruncatedHoles(string Shown, string Length);

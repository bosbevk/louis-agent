namespace LouisAgent.Composition.Web.Messages.Holes.Tavily;

/// <summary>Tavily's status and what it said.</summary>
public sealed record TavilyFailedHoles(int Status, string Detail);

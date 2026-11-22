namespace LouisAgent.Composition.Web.Messages.Holes.Google;

/// <summary>Google's status and what it said.</summary>
public sealed record GoogleFailedHoles(int Status, string Detail);

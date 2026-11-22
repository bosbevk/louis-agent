namespace LouisAgent.Composition.Web.Messages.Holes.Robots;

/// <summary>The site whose robots.txt couldn't be read, and why.</summary>
public sealed record RobotsUnreachableHoles(string Host, string Reason);

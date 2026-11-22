namespace LouisAgent.Composition.Web.Messages.Holes.Robots;

/// <summary>The site whose robots.txt says no, and the path it says no to.</summary>
public sealed record RobotsDisallowsHoles(string Host, string Path);

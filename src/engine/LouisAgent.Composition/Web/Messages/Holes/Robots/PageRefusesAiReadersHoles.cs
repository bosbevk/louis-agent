namespace LouisAgent.Composition.Web.Messages.Holes.Robots;

/// <summary>The site that says no to AI readers, and the signal that says it.</summary>
public sealed record PageRefusesAiReadersHoles(string Host, string Signal);

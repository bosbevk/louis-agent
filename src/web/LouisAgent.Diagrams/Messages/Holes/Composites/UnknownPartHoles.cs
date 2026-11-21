namespace LouisAgent.Diagrams.Messages.Holes.Composites;

/// <summary>
/// What names a part the class doesn't have, a port by its name or a connector by its other end, and the part
/// it names.
/// </summary>
public sealed record UnknownPartHoles(string By, string Part);

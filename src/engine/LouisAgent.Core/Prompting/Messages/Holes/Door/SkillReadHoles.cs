namespace LouisAgent.Core.Prompting.Messages.Holes.Door;

/// <summary>
/// A skill as the model reads it: what it does, how to use it, what it takes, and the call it was proved with.
/// </summary>
public sealed record SkillReadHoles(string Name, string Description, string Instructions, string Parameters,
    string Example);

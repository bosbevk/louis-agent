namespace LouisAgent.Core.Skills.Messages.Holes.Fields;

/// <summary>The prompt, the level it names that isn't one, and the levels there are.</summary>
public sealed record LevelUnknownHoles(string Path, string Level, string Supported);

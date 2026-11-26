namespace LouisAgent.Core.Prompting.Messages.Holes.Reading;

/// <summary>The prompt, and the key whose value is a list or a mapping where text belongs.</summary>
public sealed record ValueNotTextHoles(string Path, string Key);

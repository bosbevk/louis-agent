namespace LouisAgent.Core.Prompting.Messages.Holes.Fields;

/// <summary>The prompt, the name its header gives, and the name it must give.</summary>
public sealed record NameNotTheFilesHoles(string Path, string Name, string Expected);

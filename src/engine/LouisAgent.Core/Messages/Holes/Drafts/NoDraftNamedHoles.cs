namespace LouisAgent.Core.Messages.Holes.Drafts;

/// <summary>The name /approve or /reject was given, which no draft has.</summary>
public sealed record NoDraftNamedHoles(string Name);

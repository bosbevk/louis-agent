namespace LouisAgent.Core.Prompting.Messages.Holes.Drafting;

/// <summary>The skill whose example doesn't fit its parameters, and each problem.</summary>
public sealed record ExampleDoesNotFitHoles(string Skill, string Problems);

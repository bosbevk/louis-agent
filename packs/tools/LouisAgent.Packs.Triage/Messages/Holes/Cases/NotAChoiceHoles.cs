namespace LouisAgent.Packs.Triage.Messages.Holes.Cases;

/// <summary>A value that is none of the choices: what was given, what it should have been, and the choices.</summary>
public sealed record NotAChoiceHoles(string Value, string What, string Options);

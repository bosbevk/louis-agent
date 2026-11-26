namespace LouisAgent.Packs.Triage.Messages.Holes.Cases;

/// <summary>A case raised to a person: its run, where it is written, and the step that says so.</summary>
public sealed record RaisedHoles(string RunId, string Path, int Number);

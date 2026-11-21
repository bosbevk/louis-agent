namespace LouisAgent.Core.Agent.Messages.Holes;

/// <summary>The tool an answer still owes, for the notice that asks the model to end with it.</summary>
public sealed record EndWithToolHoles(string Tool);

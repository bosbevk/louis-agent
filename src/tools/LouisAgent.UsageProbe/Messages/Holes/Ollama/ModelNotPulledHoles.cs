namespace LouisAgent.UsageProbe.Messages.Holes.Ollama;

/// <summary>The model Ollama hasn't pulled, and where Ollama runs.</summary>
public sealed record ModelNotPulledHoles(string Model, string Endpoint);

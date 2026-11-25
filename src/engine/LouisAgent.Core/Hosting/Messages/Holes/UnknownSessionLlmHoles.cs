namespace LouisAgent.Core.Hosting.Messages.Holes;

/// <summary>An LLM profile this instance doesn't offer, and the ones it does, or <c>none</c>.</summary>
public sealed record UnknownSessionLlmHoles(string Name, string Available);

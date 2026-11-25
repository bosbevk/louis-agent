namespace LouisAgent.Core.Llm.Messages.Holes.Providers;

/// <summary>The provider LLM_PROVIDER named that the host wasn't given, and the ones it was.</summary>
public sealed record UnknownProviderHoles(string? Provider, string Supported);

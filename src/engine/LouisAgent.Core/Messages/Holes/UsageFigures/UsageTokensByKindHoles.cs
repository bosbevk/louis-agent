namespace LouisAgent.Core.Messages.Holes.UsageFigures;

/// <summary>The session's tokens by kind, each written for reading.</summary>
public sealed record UsageTokensByKindHoles(string Input, string CacheWrite, string CacheRead, string Output);

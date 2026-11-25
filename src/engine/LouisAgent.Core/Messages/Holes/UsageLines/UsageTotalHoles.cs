namespace LouisAgent.Core.Messages.Holes.UsageLines;

/// <summary>A total in words: the requests, the tokens, how many were cached, and the cost.</summary>
public sealed record UsageTotalHoles(string Requests, string Tokens, string Cached, string Cost);

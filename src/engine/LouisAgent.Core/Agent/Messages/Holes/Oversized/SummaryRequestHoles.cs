namespace LouisAgent.Core.Agent.Messages.Holes.Oversized;

/// <summary>What the summary is asked about: the user's request, the call, and the output to condense.</summary>
public sealed record SummaryRequestHoles(string Request, string Tool, string Arguments, string Output);

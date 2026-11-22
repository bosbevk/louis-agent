namespace LouisAgent.Evals.Transcripts;

/// <summary>
/// One tool call in a turn: the tool, what the agent sent it, and what it gave back as the agent got it.
/// </summary>
/// <param name="Tool">The tool's name.</param>
/// <param name="Input">What the agent sent the tool, as its JSON arguments.</param>
/// <param name="Result">What the tool gave back, whole, as the agent got it.</param>
/// <param name="Failed">Whether the call failed.</param>
public sealed record ToolCallTranscript(string Tool, string Input, string Result, bool Failed = false);

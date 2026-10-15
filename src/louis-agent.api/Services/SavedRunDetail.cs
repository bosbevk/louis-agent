namespace LouisAgent.Api.Services;

/// <summary>
/// A saved run opened (F27): its summary table (markdown), its README, its numbers (JSON, as saved), and the
/// conversations of each mode (the transcripts: the questions, the calls and the answers).
/// </summary>
public sealed record SavedRunDetail(SavedRun Run, string? Summary, string? Readme, string? Results, IReadOnlyList<RunTranscript> Transcripts);

namespace LouisAgent.Api.Services;

/// <summary>
/// A saved run opened (F27): its summary table (markdown), its README, its numbers (JSON, as saved), the conversations
/// of each mode (the transcripts), and each mode's ledger rows and answers for the drill-down.
/// </summary>
public sealed record SavedRunDetail(SavedRun Run, string? Summary, string? Readme, string? Results, IReadOnlyList<RunTranscript> Transcripts,
    IReadOnlyList<RunMode> Modes);

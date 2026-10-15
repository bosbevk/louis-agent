namespace LouisAgent.Api.Services;

/// <summary>A saved run opened (F27): its summary table (markdown), its README, and its numbers (JSON, as saved).</summary>
public sealed record SavedRunDetail(SavedRun Run, string? Summary, string? Readme, string? Results);

namespace LouisAgent.UsageProbe.Messages.Holes.Ledger;

/// <summary>The ledger read: how many requests, which models, and the prices' date.</summary>
public sealed record LedgerSummaryHoles(string Path, int Count, string Models, string AsOf);

namespace LouisAgent.UsageProbe.Messages.Holes.Ledger;

/// <summary>The sub-sessions' roll-up rows, the requests they stand for, and their cost.</summary>
public sealed record LedgerRollUpsHoles(int Rows, int Requests, string Cost);

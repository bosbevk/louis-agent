namespace LouisAgent.Evals;

/// <summary>One run of an eval: its id (the tag its sessions carry, so the ledger's rows find it) and how many times each mode repeats.</summary>
public sealed record EvalRun(string RunId, int Repeats);

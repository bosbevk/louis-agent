namespace LouisAgent.Puls.Admin.LiveRuns;

/// <summary>Which turn of a run: its mode, its repeat, its conversation and its number.</summary>
public readonly record struct TurnKey(string Mode, int Repeat, string Task, int N);

namespace LouisAgent.Api.Services;

/// <summary>One mode of a saved run (F27), for the drill-down: its ledger rows (JSON lines) and its answers (JSON), as the check saved them.</summary>
/// <param name="Name">The mode's place in the run: <c>delegated</c>, or <c>run-2/puls-web</c> for a repeated run.</param>
public sealed record RunMode(string Name, string Usage, string Answers);

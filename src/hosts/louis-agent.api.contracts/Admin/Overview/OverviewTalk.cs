namespace LouisAgent.Api.Contracts.Admin.Overview;

/// <summary>One talk, as the overview lists it (F127.8): never what was said, only where and what it did.</summary>
/// <param name="At">When its last turn ended, in UTC.</param>
/// <param name="Page">The page its last message came from.</param>
/// <param name="Part">The part of the site that page is.</param>
/// <param name="Identity">Who answered.</param>
/// <param name="Turns">How many turns it had.</param>
/// <param name="Cost">What it cost, in the reply's currency; null when none of its requests was priced.</param>
/// <param name="Tools">The tools it called, in the order first called.</param>
public sealed record OverviewTalk(
    DateTimeOffset At,
    string? Page,
    string? Part,
    string? Identity,
    int Turns,
    decimal? Cost,
    IReadOnlyList<string> Tools);

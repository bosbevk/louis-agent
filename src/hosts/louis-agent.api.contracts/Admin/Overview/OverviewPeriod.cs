namespace LouisAgent.Api.Contracts.Admin.Overview;

/// <summary>
/// One period of the overview (F127.8): its talks, what they cost, and the talks that used each tool.
/// </summary>
/// <param name="Talks">The sessions with a turn in the period.</param>
/// <param name="Cost">
/// What their requests in the period cost, in the reply's currency; null when none was priced.
/// </param>
/// <param name="Tools">Each tool called in the period and the talks that called it, the most first.</param>
public sealed record OverviewPeriod(int Talks, decimal? Cost, IReadOnlyList<OverviewBar> Tools);

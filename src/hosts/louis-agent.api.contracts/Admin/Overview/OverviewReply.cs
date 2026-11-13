namespace LouisAgent.Api.Contracts.Admin.Overview;

/// <summary>
/// What a site's agents did, for its admin's overview (F127.8): today and this month, the month's talks by part of the
/// site and by who answered, and the latest talks. Costs are in <paramref name="Currency"/>, unknown kept null.
/// </summary>
/// <param name="Currency">The currency the costs are shown in, such as ZAR.</param>
/// <param name="Today">The day asked for.</param>
/// <param name="Month">The month that day falls in.</param>
/// <param name="ByPart">The month's talks by the part of the site they were last on, the most first.</param>
/// <param name="ByIdentity">The month's talks by who answered, the most first.</param>
/// <param name="Latest">The month's latest talks, newest first.</param>
public sealed record OverviewReply(
    string Currency,
    OverviewPeriod Today,
    OverviewPeriod Month,
    IReadOnlyList<OverviewBar> ByPart,
    IReadOnlyList<OverviewBar> ByIdentity,
    IReadOnlyList<OverviewTalk> Latest);

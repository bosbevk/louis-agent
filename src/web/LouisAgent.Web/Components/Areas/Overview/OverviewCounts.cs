using LouisAgent.Api.Contracts.Admin.Overview;

namespace LouisAgent.Web.Components.Areas.Overview;

/// <summary>
/// The tools a site's admin overview counts, in the order the site named them (F127.8); none when it names none.
/// </summary>
public sealed record OverviewCounts(IReadOnlyList<CountedTool> Tools)
{
    /// <summary>What the overview calls a tool's talks, or none when the site doesn't count it.</summary>
    public string? NameOf(string tool) => Tools.FirstOrDefault(counted => counted.Tool == tool)?.Name;

    /// <summary>
    /// Each counted tool by the site's name, with the talks that called it in the period; the overview's figures and
    /// its card on the admin's home both show them (F144).
    /// </summary>
    public IReadOnlyList<OverviewBar> In(OverviewPeriod period) =>
    [
        .. Tools.Select(counted =>
            new OverviewBar(counted.Name, period.Tools.FirstOrDefault(used => used.Name == counted.Tool)?.Talks ?? 0))
    ];
}

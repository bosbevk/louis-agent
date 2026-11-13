namespace LouisAgent.Web.Components.Areas.Overview;

/// <summary>
/// The tools a site's admin overview counts, in the order the site named them (F127.8); none when it names none.
/// </summary>
public sealed record OverviewCounts(IReadOnlyList<CountedTool> Tools)
{
    /// <summary>What the overview calls a tool's talks, or none when the site doesn't count it.</summary>
    public string? NameOf(string tool) => Tools.FirstOrDefault(counted => counted.Tool == tool)?.Name;
}

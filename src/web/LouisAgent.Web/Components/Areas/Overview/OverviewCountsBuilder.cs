namespace LouisAgent.Web.Components.Areas.Overview;

/// <summary>
/// Collects the tools a site's admin overview counts (F127.8): the API counts every tool and names no pack, so the
/// site says which to show and what to call each. Its areas hand it to <see cref="SiteAreasBuilder.Overview"/>.
/// </summary>
public sealed class OverviewCountsBuilder
{
    private readonly List<CountedTool> _tools = [];

    /// <summary>Counts the talks that called <paramref name="tool"/>, called <paramref name="name"/>.</summary>
    public OverviewCountsBuilder Tool(string tool, string name)
    {
        _tools.Add(new CountedTool(tool, name));
        return this;
    }

    /// <summary>The counts collected.</summary>
    internal OverviewCounts Build() => new(_tools.ToList());
}

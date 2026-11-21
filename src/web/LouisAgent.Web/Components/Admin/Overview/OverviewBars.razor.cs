using System.Globalization;
using LouisAgent.Api.Contracts.Admin.Overview;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// The month's talks split by one thing on the admin's overview (F127.8), the part of the site or who answered: a bar
/// each, as long as its share of the largest.
/// </summary>
public partial class OverviewBars
{
    /// <summary>What the bars split by.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The bars, the most first, as the reply sends them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OverviewBar> Bars { get; set; } = [];

    /// <summary>A bar's length, as a share of the largest, written for the style.</summary>
    private string ShareOf(OverviewBar bar)
    {
        int most = Bars.Count == 0 ? 1 : Math.Max(1, Bars.Max(other => other.Talks));
        return (100m * bar.Talks / most).ToString("0", CultureInfo.InvariantCulture);
    }
}

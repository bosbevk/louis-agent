using System.Globalization;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// The latest talks on the admin's overview (F127.8): a row each with when it ended in the reader's time, its page and
/// part, who answered, its turns, its cost, and the counted tools it called, by the site's names. Never what was said.
/// </summary>
public partial class LatestTalks
{
    /// <summary>The talks, newest first.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OverviewTalk> Talks { get; set; } = [];

    /// <summary>The tools the site counts.</summary>
    [Parameter, EditorRequired]
    public OverviewCounts Counts { get; set; } = default!;

    /// <summary>The currency the reply's costs are in.</summary>
    [Parameter, EditorRequired]
    public string Currency { get; set; } = "";

    /// <summary>The reader's currency, when they chose one.</summary>
    [Parameter]
    public CurrencyChoice? Choice { get; set; }

    /// <summary>When the talk ended, in the reader's own time (ADR 0046: the screen converts).</summary>
    private static string WhenOf(OverviewTalk talk) =>
        talk.At.ToLocalTime().ToString("d MMM HH:mm", CultureInfo.InvariantCulture);

    /// <summary>The counted tools the talk called, by the site's names.</summary>
    private IEnumerable<string> CountedIn(OverviewTalk talk) =>
        talk.Tools.Select(Counts.NameOf).OfType<string>().Distinct();
}

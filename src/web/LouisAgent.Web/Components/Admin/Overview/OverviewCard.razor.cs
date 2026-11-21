using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// The overview's card on the admin's home (F144): how many talks today, what they cost, and the tools the site
/// counts, with the way to the whole overview.
/// </summary>
/// <remarks>
/// It reads the same overview the page does and shows what it last fetched (ADR 0042); a refused key, a saved one and
/// a lost API are <see cref="AdminCardBase"/>'s to handle.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class OverviewCard
{
    private OverviewReply? _reply;

    [Inject] private CurrencyState Currency { get; set; } = null!;
    [Inject] private OverviewCounts Counts { get; set; } = null!;

    /// <summary>The reader's currency, then today's overview.</summary>
    protected override async Task ReadAsync()
    {
        await Currency.LoadAsync();
        _reply = await Api.GetAsync<OverviewReply>("v1/admin/overview", AdminJson.SnakeCase);
    }
}

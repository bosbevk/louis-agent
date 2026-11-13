using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// The overview's card on the admin's home (F144): how many talks today, what they cost, and the tools the site
/// counts, with the way to the whole overview.
/// </summary>
/// <remarks>
/// It reads the same overview the page does and shows what it last fetched (ADR 0042). When the API says no, the card
/// stays quiet and the home asks for the key, so one refusal isn't asked about once per card.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class OverviewCard
{
    private OverviewReply? _reply;
    private bool _notRead;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;
    [Inject] private OverviewCounts Counts { get; set; } = null!;

    /// <summary>Loads the admin key and the reader's currency, then today's overview.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await Currency.LoadAsync();
        try
        {
            _reply = await Api.GetAsync<OverviewReply>("v1/admin/overview", AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            _notRead = true;
        }
        catch (HttpRequestException)
        {
            _notRead = true;
        }
    }
}

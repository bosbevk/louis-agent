using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Evals;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage.Details;

/// <summary>
/// The details pane of the Evals page: everything about the chosen eval. What the list reply carries is placed
/// through <see cref="EvalCardView" />; the trend is fetched here, once per eval.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class EvalDetails
{
    /// <summary>The eval's trend; none when the API has none for it.</summary>
    private TrendReply? _trend;

    /// <summary>The eval the trend was fetched for, so another eval fetches its own.</summary>
    private string? _trendOf;

    /// <summary>The API the trend is read from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The eval, as the list reply carries it.</summary>
    [Parameter, EditorRequired]
    public EvalRow Eval { get; set; } = null!;

    /// <summary>The currency money is shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Says the eval's drafts changed, so the page reads the list again.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>What the pane shows of the list reply.</summary>
    private EvalCardView View { get; set; } = null!;

    /// <summary>Builds the view, and fetches the trend when the eval is another than the one shown.</summary>
    protected override async Task OnParametersSetAsync()
    {
        View = EvalCardView.Of(Eval);
        if (_trendOf == Eval.Name) return;
        _trendOf = Eval.Name;
        _trend = await TrendAsync(Eval.Name);
    }

    /// <summary>The eval's trend; none when the API can't give one, and the pane shows no trend.</summary>
    private async Task<TrendReply?> TrendAsync(string eval)
    {
        try
        {
            return await Api.GetAsync<TrendReply?>($"v1/admin/evals/{Uri.EscapeDataString(eval)}/trend",
                AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            return null;
        }
    }
}

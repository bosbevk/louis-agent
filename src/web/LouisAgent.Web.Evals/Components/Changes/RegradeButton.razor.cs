using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Changes;

/// <summary>
/// Re-grades a run that has ended: the run's stored answers graded again against the eval file as it is now. It
/// starts at once and takes the person to the new run's page, which says what it did and what it cost.
/// </summary>
/// <remarks>
/// No ask before it starts (Louis's pick, 2026-11-23, "No ask, the run shows it after"): a re-grade asks the judge
/// only for what is new, so it costs little, and the run says after what it spent. The button is off from a press
/// until the API answers, so a double press starts one re-grade.
/// </remarks>
public partial class RegradeButton
{
    private bool _starting;
    private string? _refused;

    /// <summary>The eval the run belongs to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run's id.</summary>
    [Parameter, EditorRequired]
    public string Run { get; set; } = "";

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private static string Regrade => EvalsWords.Words[EvalsPart.Regrade];

    /// <summary>Starts the re-grade and goes to its page; a refusal stays in the API's words.</summary>
    private async Task RegradeAsync()
    {
        if (_starting) return;
        _starting = true;
        _refused = null;
        try
        {
            var start = await Api.RegradeAsync(Run);
            if (start.Run is { } regrade)
            {
                Navigation.NavigateTo(EvalAddress.RunPageOf(Eval, regrade));
                return;
            }

            _refused = start.Refused;
        }
        catch (AgentApiException refused)
        {
            _refused = refused.Message;
        }

        _starting = false;
    }
}

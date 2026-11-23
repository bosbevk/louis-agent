using System.Net;
using LouisAgent.Choices;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Pages;

/// <summary>
/// A run's page. It reads the run whole, the host's evals and this eval's runs when it opens and on Reload, and asks
/// the run's own records for every figure it shows: passed, the marks met, the cost and the time.
/// </summary>
/// <remarks>
/// The page only draws. What a run knows about itself (its counts, its order failed first) is the record's to
/// answer (ADR 0123), so the conversation's page and the API ask the same methods and can't disagree.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class RunPage
{
    private EvalRun? _run;
    private EvalRow? _eval;
    private IReadOnlyList<RunRow> _runs = [];
    private string? _error;
    private bool _needsKey, _loading, _missing;

    /// <summary>The eval the run belongs to, from the address.</summary>
    [Parameter]
    public string Eval { get; set; } = "";

    /// <summary>The run's id, from the address.</summary>
    [Parameter]
    public string Run { get; set; } = "";

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    private int Passed => _run?.Passed ?? 0;

    /// <summary>How many conversations the run has graded.</summary>
    private int Graded => _run?.Conversations.Count ?? 0;

    /// <summary>How many conversations the eval holds, or the run's own count while the eval isn't listed.</summary>
    private int Held => _eval?.Conversations ?? Graded;

    /// <summary>"2 conversations", the ones of the eval the run didn't reach.</summary>
    private string NotRun =>
        EvalsWords.Count(_run is null ? 0 : _eval?.CountNotRunIn(_run) ?? 0, EvalsPart.Conversation,
            EvalsPart.Conversations);

    /// <summary>The run's kind, as its file writes it.</summary>
    private string Kind => _run?.Kind.Wire() ?? "";

    /// <summary>How long the run took, or unknown while it has no end.</summary>
    private string Took => _run?.MeasureTime() is { } time ? EvalsWords.FormatTime(time) : Unknown;

    /// <summary>What the agent's turns cost, or unknown when none was priced, never 0 (ADR 0007).</summary>
    private string Cost => _run?.SumCost() is { } cost ? UsageFormat.Money(cost, _run.FindCurrency()) : Unknown;

    private static string Unknown => EvalsWords.Words[EvalsPart.Unknown];

    /// <summary>Loads the admin key, then reads the run.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>
    /// Reads the run, the eval's line in the host's list and the eval's runs. A run the host doesn't have is one line;
    /// a refused key shows the frame's key box, and a host that can't be reached says so in a line.
    /// </summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _run = await Api.FindRunAsync(Run);
            _missing = _run is null;
            _eval = (await Api.ListAsync()).Evals.FirstOrDefault(row => row.Name == Eval);
            _runs = await Api.ListRunsAsync(Eval);
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
    }
}

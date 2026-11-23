using System.Net;
using LouisAgent.Choices;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
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
/// the run's own records for every figure it shows: passed, the marks met, both costs and the time. A going run is
/// read again every few seconds and can be stopped (F126.3.4); one that has ended can be re-graded, and a re-grade is
/// read beside the run it re-graded, to say what it did (F126.4).
/// </summary>
/// <remarks>
/// The page only draws. What a run knows about itself (its counts, its order failed first) is the record's to
/// answer (ADR 0123), so the conversation's page and the API ask the same methods and can't disagree.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class RunPage
{
    private EvalRun? _run, _before;
    private EvalRow? _eval;
    private IReadOnlyList<RunRow> _runs = [];
    private string? _error, _read;
    private bool _needsKey, _loading, _missing, _stopping, _gone;
    private ITimer? _timer;

    /// <summary>How often a going run is read again: a run's file changes about once a conversation.</summary>
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(3);

    /// <summary>The eval the run belongs to, from the address.</summary>
    [Parameter]
    public string Eval { get; set; } = "";

    /// <summary>The run's id, from the address.</summary>
    [Parameter]
    public string Run { get; set; } = "";

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>The clock the page follows a run by, the one the evals' area registers.</summary>
    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    /// <summary>Whether the run is going, so the page follows it and offers to stop it.</summary>
    private bool Going => _run is { Status: RunStatus.Running };

    private int Passed => _run?.Passed ?? 0;

    /// <summary>The run against the run before, asked of the run (F126.4); none with no run before.</summary>
    private RunChange? Change => _run is { } run && _before is { } before ? run.CompareWith(before) : null;

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

    /// <summary>"So far" while the run goes, "Took" once it has ended.</summary>
    private string TimeName => EvalsWords.Words[Going ? EvalsPart.SoFar : EvalsPart.Took];

    /// <summary>How long it has gone, by the page's clock while it goes, or unknown before it is read.</summary>
    private string Time => _run is { } run
        ? EvalsWords.FormatTime(run.MeasureTimeSoFar(Clock.GetUtcNow()))
        : Unknown;

    private static string AgentCostName => EvalsWords.Words[EvalsPart.AgentCostName];

    private static string JudgeCostName => EvalsWords.Words[EvalsPart.JudgeCostName];

    private static string StopTheRun => EvalsWords.Words[EvalsPart.StopTheRun];

    private string GoingHead => EvalsWords.Fill(EvalsPart.RunGoingHead, Graded, Held);

    private string StoppedHead => EvalsWords.Fill(EvalsPart.RunStoppedHead, Graded, Held);

    /// <summary>What judging its turns cost so far, or unknown when none is known, never 0 (ADR 0007).</summary>
    private string JudgeCost => _run?.SumJudgeCost() is { } cost
        ? UsageFormat.Money(cost, _run.FindCurrency())
        : Unknown;

    /// <summary>What the agent's turns cost, or unknown when none was priced, never 0 (ADR 0007).</summary>
    private string Cost => _run?.SumCost() is { } cost ? UsageFormat.Money(cost, _run.FindCurrency()) : Unknown;

    private static string Unknown => EvalsWords.Words[EvalsPart.Unknown];

    /// <summary>
    /// Loads the admin key, then reads the run when the address names one not read yet: Blazor keeps this page when
    /// only the run in its address changes, as after a re-grade, so the run is read on its parameters, not once.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        if (_read == Run) return;

        StopFollowing();
        await LoadAsync();
        Follow();
    }

    /// <summary>The page is gone: it reads no more.</summary>
    public void Dispose()
    {
        _gone = true;
        StopFollowing();
    }

    /// <summary>
    /// Follows a going run: a timer on the page's clock reads it again every few seconds, until it has ended or the
    /// page is gone. A finished run's page never reads by itself.
    /// </summary>
    private void Follow()
    {
        if (_gone || !Going)
        {
            StopFollowing();
            return;
        }

        // The tick comes off the render thread, so the read and the redraw go through the renderer.
        _timer ??= Clock.CreateTimer(_ => _ = InvokeAsync(ReadAgainAsync), null, Every, Every);
    }

    /// <summary>One tick: the run read again, drawn, and followed on only while it still goes.</summary>
    private async Task ReadAgainAsync()
    {
        if (_gone) return;
        await LoadAsync();
        Follow();
        StateHasChanged();
    }

    private void StopFollowing()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>Stops the run, then reads it again to show how it ended.</summary>
    private async Task StopAsync()
    {
        _stopping = true;
        await Api.StopAsync(Run);
        await LoadAsync();
        _stopping = false;
        Follow();
    }

    /// <summary>
    /// The run this one is compared with, read whole (F126.4): for a re-grade the run it re-graded, else the newest
    /// earlier run that finished, as the eval's history finds it; none when there is no such run.
    /// </summary>
    private async Task<EvalRun?> ReadBeforeAsync()
    {
        if (_runs.FirstOrDefault(row => row.Id == Run) is not { } row) return null;
        if (new RunHistory(_runs).FindBefore(row) is not { } before) return null;

        return await Api.FindRunAsync(before.Id);
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
            _before = await ReadBeforeAsync();
            _read = Run;
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

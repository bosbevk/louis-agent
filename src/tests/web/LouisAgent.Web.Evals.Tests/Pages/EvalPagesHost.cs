using System.Net;
using System.Net.Http.Json;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// An eval page (a run's or a conversation's) over a stub of the API that answers each eval route the pages read:
/// the run, the host's evals and the eval's runs. It counts the run's reads, so a test can see Reload read again.
/// </summary>
internal sealed class EvalPagesHost
{
    private EvalRun? _run;
    private readonly EvalsReply _evals;
    private readonly IReadOnlyList<RunRow> _runs;

    /// <summary>A host with this run, or none, these evals and these runs of the eval.</summary>
    public EvalPagesHost(EvalRun? run, EvalsReply? evals = null, IReadOnlyList<RunRow>? runs = null)
    {
        _run = run;
        _evals = evals ?? new EvalsReply([]);
        _runs = runs ?? [];
    }

    /// <summary>How many times the run itself was read.</summary>
    public int RunReads { get; private set; }

    /// <summary>How many times the run was asked to stop.</summary>
    public int Stops { get; private set; }

    /// <summary>The clock the pages are given, which a test moves (F126.3.4).</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 23, 14, 10, 0, TimeSpan.Zero));

    /// <summary>From now on the run is answered as this, as a run that goes on changes.</summary>
    public void Answers(EvalRun run) => _run = run;

    /// <summary>The page for this eval's run, once it has loaded.</summary>
    public IRenderedComponent<RunPage> Render(BunitContext ctx, string eval, string run) =>
        Render<RunPage>(ctx, parameters => parameters.Add(p => p.Eval, eval).Add(p => p.Run, run), ".run-loading");

    /// <summary>A page given these parameters, once its loading line, found by this selector, has gone.</summary>
    public IRenderedComponent<TPage> Render<TPage>(BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<TPage>> parameters, string loading) where TPage : IComponent
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var http = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        var api = new AgentApi(http, settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new EvalsApi(api));
        ctx.Services.AddSingleton(new AdminAccess());
        ctx.Services.AddSingleton<TimeProvider>(Clock);

        var page = ctx.Render(parameters);
        page.WaitForState(() => page.FindAll(loading).Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>Answers a route as the API would: the runs list, the run (none is a 404), or the evals.</summary>
    private HttpResponseMessage Answer(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/runs", StringComparison.Ordinal)) return Json(_runs);
        if (!path.Contains("/runs/", StringComparison.Ordinal)) return Json(_evals);

        if (request.Method == HttpMethod.Delete)
        {
            Stops++;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        RunReads++;
        return _run is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(_run);
    }

    /// <summary>An answer of this value, written as the routes write it.</summary>
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: RunJson.Options) };
}

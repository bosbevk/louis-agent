using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Evals.Contracts.Grading;
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
/// An eval page (a run's or a conversation's) over a stub of the API that answers each eval route the pages read and
/// call: the run, the host's evals, the eval's runs, a stop and a mark's flip. It counts the run's reads, so a test
/// can see Reload read again, and changes its run on a flip as the API does, keeping what each flip sent.
/// </summary>
internal sealed class EvalPagesHost
{
    private EvalRun? _run;
    private readonly Dictionary<string, EvalRun> _others = [];
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

    /// <summary>F126.4: each flip or undo sent, its method and the note it carried.</summary>
    public List<(HttpMethod Method, string? Note)> Flips { get; } = [];

    /// <summary>When set, every flip and undo is refused with this reason, as the API refuses one.</summary>
    public string? RefusesFlips { get; set; }

    /// <summary>The clock the pages are given, which a test moves (F126.3.4).</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 23, 14, 10, 0, TimeSpan.Zero));

    /// <summary>From now on the run is answered as this, as a run that goes on changes.</summary>
    public void Answers(EvalRun run) => _run = run;

    /// <summary>F126.4: another run the host keeps, answered at its own address, as the run before is read.</summary>
    public EvalPagesHost Keeps(EvalRun other)
    {
        _others[other.Id] = other;
        return this;
    }

    /// <summary>F126.4: how many re-grades were started.</summary>
    public int Regrades { get; private set; }

    /// <summary>When set, a re-grade is refused with this reason, as the API refuses one.</summary>
    public string? RefusesRegrades { get; set; }

    /// <summary>The id a re-grade started here takes.</summary>
    public const string RegradeId = "20261123-170000-guide";

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
        if (path.EndsWith("/flip", StringComparison.Ordinal)) return Flip(request, path);
        if (path.EndsWith("/regrade", StringComparison.Ordinal)) return Regrade();
        if (_others.TryGetValue(Uri.UnescapeDataString(path.Split('/')[^1]), out var other)) return Json(other);

        if (request.Method == HttpMethod.Delete)
        {
            Stops++;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        RunReads++;
        return _run is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(_run);
    }

    /// <summary>
    /// A flip at a mark's address, <c>…/runs/{id}/{conversation}/{repeat}/{turn}/criteria/{criterion}/flip</c>: the
    /// run changed as the API changes it, or the refusal a test asked for.
    /// </summary>
    private HttpResponseMessage Flip(HttpRequestMessage request, string path)
    {
        string? note = request.Content is null
            ? null
            : JsonDocument.Parse(request.Content.ReadAsStringAsync().Result).RootElement.GetProperty("note")
                .GetString();
        Flips.Add((request.Method, note));
        if (RefusesFlips is { } reason) return Refused(reason);

        string[] parts = path.Split('/');
        var flip = request.Method == HttpMethod.Put ? new Flip(Clock.GetUtcNow(), note) : null;
        _run = _run!.WithFlip(Uri.UnescapeDataString(parts[^6]), int.Parse(parts[^5]), int.Parse(parts[^4]),
            int.Parse(parts[^2]), flip);
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    /// <summary>A re-grade started, at its new run's address, or the refusal a test asked for.</summary>
    private HttpResponseMessage Regrade()
    {
        Regrades++;
        if (RefusesRegrades is { } reason) return Refused(reason);

        var accepted = new HttpResponseMessage(HttpStatusCode.Accepted);
        accepted.Headers.Location = new Uri($"/v1/admin/evals/runs/{RegradeId}", UriKind.Relative);
        return accepted;
    }

    /// <summary>A 409 with this reason, in the API's own error shape.</summary>
    private static HttpResponseMessage Refused(string reason)
    {
        var refusal = new ApiError("error", new ApiErrorDetail("a-refusal", reason));
        return new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(refusal, options: JsonSerializerOptions.Web)
        };
    }

    /// <summary>An answer of this value, written as the routes write it.</summary>
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: RunJson.Options) };
}

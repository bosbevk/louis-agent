using LouisAgent.Composition.Messages;
using LouisAgent.Api.Endpoints.Admin.Drafts;
using LouisAgent.Api.Endpoints.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Imports;
using LouisAgent.Api.Endpoints.Admin.Prompts;
using LouisAgent.Api.Endpoints.Admin.Reports;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using LouisAgent.Api.Endpoints.Admin.Screens;
using LouisAgent.Api.Endpoints.Admin.Studio;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Hangs every admin portal route on the API: the checks, starting a run, and reading back what it recorded. All of
/// them sit behind the admin key, and without a store no run starts.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class AdminEndpoints
{
    /// <summary>
    /// Where the eval routes hang: <c>/evals</c>, and <c>/benchmarks</c>, their old name, kept as an alias until the
    /// TODO line that removes it.
    /// </summary>
    private static readonly string[] Prefixes = ["/evals", "/benchmarks"];

    /// <summary>
    /// Adds the admin routes under <c>/admin</c>, behind the key. The clock stamps the notes, so a test can fix it.
    /// </summary>
    public static void MapAdmin(this IEndpointRouteBuilder routes, EvalList catalog, EvalProcess runner,
        string? adminKey,
        IEvalStore? store, EvalRecorder? recorder,
        Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf,
        string workspaceRoot, IHumanReviewStore? reviews = null, ICalibrationStore? calibrations = null,
        CalibrationRecorder? calibrationRecorder = null,
        TimeProvider? time = null, IEvalDraftStore? drafts = null, StudioRunner? studio = null)
    {
        var handlers = new AdminHandlers(catalog, runner, adminKey, store, recorder, settingsOf, workspaceRoot);
        // One group for all of them, so the key check runs before any route's own work.
        var admin = routes.MapGroup("/admin").WithAccess(RouteAccess.AdminKey).AddEndpointFilter(handlers.Gate);

        // One draft store for the list and both names, so a draft made under one is listed under the other.
        var draftStore = drafts ?? new InMemoryEvalDraftStore();

        // The two lists: every eval (GET /evals), and the checks with their latest runs the Benchmarks page reads.
        admin.MapEvals(catalog, store, workspaceRoot, draftStore);
        admin.MapGet("/benchmarks", handlers.GetList);

        // Every built-in prompt as its header says it (CL27).
        admin.MapGet("/prompts", PromptsHandlers.GetPrompts);

        // The data a person uploads into the packs, previewed, then saved (F55).
        admin.MapImports();

        // The numbers the packs keep about themselves (F35.7).
        admin.MapReports();

        // The screens the packs describe, drawn by one generic page.
        admin.MapScreens();

        // The studio (F49.5): the runner's catalogue, its check and promptfoo in and out, which say the runner isn't
        // there when none was given, and the evals being composed, kept beside the other drafts.
        // An eval the runner lists is edited in its own folder (FX32).
        var runnerParts = studio ?? StudioRunner.None;
        var homes = new EvalHomes(catalog, workspaceRoot);
        admin.MapStudio(runnerParts, draftStore, time ?? TimeProvider.System, homes);

        // A draft's try is a run like the Run button's, recorded with the settings every run reports.
        var trying = new DraftTryHandlers(draftStore, runner, recorder, settingsOf, StudioEndpoints.Scratch, homes);
        admin.MapPost("/evals/drafts/{id}/try", trying.PostTry);

        // Its publish: the checked folder on a branch in the workspace, or a zip where there's no git workspace.
        var publishing = new DraftPublishHandlers(draftStore, runnerParts.Checker, homes, StudioEndpoints.Scratch);
        admin.MapPost("/evals/drafts/{id}/publish", publishing.PostPublish);

        // The eval routes under their name, and again under their old one, so a saved link or an older page keeps
        // working until a TODO line removes the old name.
        foreach (string prefix in Prefixes)
        {
            var evals = admin.MapGroup(prefix);
            evals.MapGet("/{check}/manual", handlers.GetManual);
            evals.MapStoredRuns(store, runner, reviews, calibrations);
            evals.MapReviews(store, reviews);
            evals.MapNotes(store, time ?? TimeProvider.System);
            evals.MapRescore(catalog, runner, store, recorder, settingsOf);
            evals.MapCalibrations(catalog, runner, store, reviews, calibrations, calibrationRecorder);
            evals.MapDrafts(catalog, store, reviews, draftStore, workspaceRoot, time ?? TimeProvider.System);
            evals.MapPost("/{check}/runs", handlers.StartRun);
            evals.MapGet("/runs/current", handlers.GetCurrent);
            evals.MapGet("/runs/{id}/events", handlers.GetEvents);
            // What a run that's going has asked so far, so the page prices each turn as it ends (FX34).
            evals.MapGet("/runs/{id}/requests", handlers.GetRequests);
        }

        // An eval's trend: its new address, and the old one that had a "checks" in it.
        admin.MapGet("/evals/{check}/trend", handlers.GetTrend);
        admin.MapGet("/benchmarks/checks/{check}/trend", handlers.GetTrend);
    }

    /// <summary>What we tell the person when the store didn't answer, with its reason.</summary>
    internal static string Unavailable(Exception ex) =>
        MessageText.Format(ApiUserText.BenchmarkStoreUnavailable, new { Reason = ex.Message });
}

/// <summary>
/// What the page sends to start a run: how many repeats, and for an eval what it does with the web. It can record the
/// web (or turn it off), or replay an earlier run's answers, but not both.
/// </summary>
internal sealed record StartRunRequest(int? Repeat, string? Web = null, string? Replay = null);

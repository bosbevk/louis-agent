using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using Microsoft.AspNetCore.Http.HttpResults;
using SampleAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Drafts.SampleDraftRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using TranscriptAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Drafts.TranscriptDraftRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ScenarioAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Drafts.ScenarioRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Drafts;

/// <summary>
/// Makes drafts from a stored run: a sample from one of its conversations and the scenario it ran, a reference or
/// known-bad transcript from a conversation people reviewed, and the scenario itself for the sample dialog to start
/// from. A draft never changes the eval: it waits in the store for F49.5's publish.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>ReviewHandlers</c>: the routes
/// share the catalogue, the stores, the workspace and the clock, which aren't services, so they live here and each
/// route is a named method a test can call with no HTTP. Guard clauses, one per refusal, in the order a person would
/// fix them; the factories' own refusals (an id, a key, a kind the verdicts don't back) come back as a 400 with their
/// words. A new draft answers <c>CreatedJson</c>, a 201 on the admin wire.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
internal sealed class DraftHandlers(
    EvalList catalog,
    IEvalStore? store,
    IHumanReviewStore? reviews,
    IEvalDraftStore drafts,
    string workspaceRoot,
    TimeProvider time)
{
    /// <summary>Makes a sample draft from one of the run's conversations, over the scenario it ran.</summary>
    public async Task<SampleAnswer> PostSample(string id, SampleDraftRequest? request,
        CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        if (request?.Conversation is not { } named) return Bad(MessageText.Format(DraftMessage.NeedsAConversation));
        if (string.IsNullOrWhiteSpace(request.Author)) return Bad(MessageText.Format(DraftMessage.AuthorBlank));
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var key = named.ToKey();
            if (!await HasConversationAsync(store, id, key, cancellationToken)) return NoConversation(id, key);
            var eval = EvalOf(run);
            if (Scenario(eval, key.Task) is not { } scenario) return NoScenario(run.Check, key.Task);

            var turns = await store.TurnsAsync(id, key, cancellationToken);
            var choices = new SampleChoices(request.Id ?? "", request.Expected, request.Categories, request.Target,
                request.Author.Trim());
            var made = Make(() => SampleDrafts.Of(ShortId.New(), run.Check, eval?.Eval?.Identity ?? "", id, key,
                turns, scenario, choices, time.GetUtcNow()));
            if (made.Refusal is { } refusal) return Bad(refusal);

            // Made, so the draft is there.
            var draft = made.Draft!;
            await drafts.AddAsync(draft, cancellationToken);
            return new CreatedJson<SampleDraftRow>(DraftPath(run.Check, draft.Id), SampleDraftRow.Of(draft),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Keeps one of the run's reviewed conversations as a reference or a known-bad transcript draft.</summary>
    public async Task<TranscriptAnswer> PostTranscript(string id, TranscriptDraftRequest? request,
        CancellationToken cancellationToken)
    {
        if (store is null || reviews is null) return NoStore();
        if (request?.Conversation is not { } named) return Bad(MessageText.Format(DraftMessage.NeedsAConversation));
        if (Kind(request.Kind) is not { } kind) return UnknownKind(request.Kind ?? "");
        if (string.IsNullOrWhiteSpace(request.Author)) return Bad(MessageText.Format(DraftMessage.AuthorBlank));
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var key = named.ToKey();
            if (!await HasConversationAsync(store, id, key, cancellationToken)) return NoConversation(id, key);

            // People's decided verdicts on this conversation, and its lines as a transcript file holds them.
            var verdicts = HumanGrades.Decided(await reviews.OfRunAsync(id, cancellationToken))
                .Where(g => g.Conversation == key).Select(g => g.Verdict).ToList();
            var lines = await EvalProtocolExport.ConversationAsync(store, run, key, cancellationToken);
            var made = Make(() => TranscriptDrafts.Of(ShortId.New(), run.Check, EvalOf(run)?.Eval?.Identity ?? "",
                id, key, verdicts, kind, request.Reason, lines, request.Author.Trim(), time.GetUtcNow()));
            if (made.Refusal is { } refusal) return Bad(refusal);

            // Made, so the draft is there.
            var draft = made.Draft!;
            await drafts.AddAsync(draft, cancellationToken);
            return new CreatedJson<TranscriptDraftRow>(DraftPath(run.Check, draft.Id), TranscriptDraftRow.Of(draft),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>The scenario one of the run's conversations ran, for the sample dialog to start from.</summary>
    public async Task<ScenarioAnswer> GetScenario(string id, string task, CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            if (Scenario(EvalOf(run), task) is not { } scenario) return NoScenario(run.Check, task);
            return TypedResults.Json(ScenarioRow.Of(scenario), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>A draft, or the words a factory refused it with.</summary>
    private sealed record Made<T>(T? Draft, string? Refusal);

    /// <summary>Runs a factory, turning its refusal into the words the person reads.</summary>
    private static Made<T> Make<T>(Func<T> factory) where T : class
    {
        try
        {
            return new Made<T>(factory(), null);
        }
        catch (ArgumentException ex)
        {
            return new Made<T>(null, ex.Message);
        }
    }

    /// <summary>The catalogue's eval the run ran, if the runner still lists it.</summary>
    private EvalEntry? EvalOf(EvalDocuments.Run run) => catalog.Checks.FirstOrDefault(c => c.Id == run.Check);

    /// <summary>The scenario by that name in the eval's folder, if there is one.</summary>
    private SampleBody? Scenario(EvalEntry? eval, string task) =>
        ScenarioFiles.Read(workspaceRoot, eval?.Eval?.Folder, task);

    /// <summary>Whether the run stored that conversation.</summary>
    private static async Task<bool> HasConversationAsync(IEvalStore s, string id, ConversationKey key,
        CancellationToken cancellationToken) =>
        (await s.ConversationsAsync(id, cancellationToken)).Any(c => c.Key == key);

    /// <summary>Reads the kind from its word. A number never counts, even one an enum would take.</summary>
    private static TranscriptKind? Kind(string? text)
    {
        if (string.IsNullOrEmpty(text) || int.TryParse(text, out _)) return null;
        return SettingChoice.TryParse(text, out TranscriptKind? kind) ? kind : null;
    }

    /// <summary>Where a new draft can be found: under its eval's drafts.</summary>
    private static string DraftPath(string eval, string draft) =>
        $"/v1/admin/evals/{Uri.EscapeDataString(eval)}/drafts/{draft}";

    /// <summary>The 400 for a kind of transcript we don't know, listing the ones we do.</summary>
    private static JsonHttpResult<ApiError> UnknownKind(string text) =>
        Bad(MessageText.Format(DraftMessage.TranscriptKindUnknown,
            new
            {
                Kind = text,
                Supported = string.Join(", ", SettingChoice.Names<TranscriptKind>())
            }));

    /// <summary>The 404 for a scenario the eval's folder doesn't have.</summary>
    private static JsonHttpResult<ApiError> NoScenario(string eval, string scenario) =>
        ApiErrors.NotFound(MessageText.Format(DraftMessage.NoScenario,
            new
            {
                Eval = eval,
                Scenario = scenario
            }));

    /// <summary>The 404 for a conversation the run didn't store.</summary>
    private static JsonHttpResult<ApiError> NoConversation(string id, ConversationKey key) =>
        ApiErrors.NotFound(MessageText.Format(ReviewUserText.NoStoredConversation,
            new
            {
                Id = id,
                key.Mode,
                key.Repeat,
                key.Task
            }));

    /// <summary>A 400 that says what was wrong.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) => ApiErrors.BadRequest(message);

    /// <summary>The 503 when there's no store at all.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Unavailable(MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 503 when the store didn't answer, with its reason.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));

    /// <summary>The 404 for a run the store doesn't hold.</summary>
    private static JsonHttpResult<ApiError> NoRun(string id) =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
}

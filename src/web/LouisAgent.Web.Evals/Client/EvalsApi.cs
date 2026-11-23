using System.Net;
using System.Net.Http.Json;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;

namespace LouisAgent.Web.Evals.Client;

/// <summary>
/// Reads the eval routes for the pages: the host's evals, one run whole, and an eval's runs; and calls them, to
/// start or stop a run and to flip a mark. It reads with the run's own JSON options, the ones the store wrote it
/// with, strict as every reader of ours.
/// </summary>
/// <remarks>
/// A thin client over the site's API client, as the accounts' is. A run the host doesn't have is none, since a page
/// says so in its own words. Everything else, a refused key or a host that can't be reached, is left to the API
/// client's own exception, which every admin page already turns into the key box or a plain line.
/// </remarks>
/// <param name="api">The site's API client, with the admin's key.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public sealed class EvalsApi(AgentApi api)
{
    /// <summary>The host's evals, each with its last run in short.</summary>
    public Task<EvalsReply> ListAsync() => api.GetAsync<EvalsReply>(EvalAddress.EvalsRoute, RunJson.Options);

    /// <summary>The run with this id, as it is kept; none when the host doesn't have it.</summary>
    public async Task<EvalRun?> FindRunAsync(string id)
    {
        try
        {
            return await api.GetAsync<EvalRun>(EvalAddress.RunRouteOf(id), RunJson.Options);
        }
        catch (AgentApiException missing) when (missing.Status == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Starts a run of the eval: its kind and its repeats posted to the eval's runs, written as the routes read them.
    /// Answers the run's id from the address the API gave, or, when the API said no, its own reason word for word. A
    /// refused key throws, as every call here does.
    /// </summary>
    public Task<RunStart> StartAsync(string eval, RunKind kind, int repeats)
    {
        var asked = new StartRunRequest(kind, repeats);
        var start = new HttpRequestMessage(HttpMethod.Post, EvalAddress.RunsRouteOf(eval));
        start.Content = JsonContent.Create(asked, options: RunJson.Options);
        return SendStartAsync(start);
    }

    /// <summary>
    /// Re-grades a kept run (F126.4), answering as a start does: the new run's id, or the API's reason when it
    /// refused; a refused key throws.
    /// </summary>
    public Task<RunStart> RegradeAsync(string run) =>
        SendStartAsync(new HttpRequestMessage(HttpMethod.Post, EvalAddress.RegradeRouteOf(run)));

    /// <summary>Sends a start or a re-grade: the run's id from the address the API gave, or its reason.</summary>
    private async Task<RunStart> SendStartAsync(HttpRequestMessage start)
    {
        using var sent = start;
        try
        {
            using var started = await api.SendAsync(sent);
            return new RunStart(EvalAddress.FindRunId(started.Headers.Location?.OriginalString ?? ""), null);
        }
        catch (AgentApiException refused) when (IsARefusal(refused.Status))
        {
            return new RunStart(null, refused.Message);
        }
    }

    /// <summary>
    /// A run's conversation as a block for its eval file, to copy and change (F126.4); a run or a conversation the
    /// host doesn't have, or a refused key, throws.
    /// </summary>
    public Task<string> FindAsFileAsync(string run, string conversation) =>
        api.GetTextAsync(EvalAddress.AsFileRouteOf(run, conversation));

    /// <summary>
    /// Stops the run. A run the host doesn't have, or one that has already ended, needs no stop, so neither throws;
    /// a refused key does.
    /// </summary>
    public async Task StopAsync(string run)
    {
        using var stop = new HttpRequestMessage(HttpMethod.Delete, EvalAddress.RunRouteOf(run));
        try
        {
            using var _ = await api.SendAsync(stop);
        }
        catch (AgentApiException over) when (over.Status is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            // Nothing to stop: the page reads the run again and shows how it ended.
        }
    }

    /// <summary>
    /// Flips a criterion's mark, with a note or none (F126.4). Answers none when the API took it, or its own reason,
    /// word for word, when it refused; a refused key throws.
    /// </summary>
    public Task<string?> FlipAsync(MarkPlace place, string? note)
    {
        var flip = new HttpRequestMessage(HttpMethod.Put, EvalAddress.FlipRouteOf(place));
        var body = new FlipBody(note);
        flip.Content = JsonContent.Create(body, options: RunJson.Options);
        return SendFlipAsync(flip);
    }

    /// <summary>Puts the judge's answer back on a flipped mark, answering as a flip does.</summary>
    public Task<string?> UndoFlipAsync(MarkPlace place) =>
        SendFlipAsync(new HttpRequestMessage(HttpMethod.Delete, EvalAddress.FlipRouteOf(place)));

    /// <summary>Sends a flip or its undo: none when it was taken, or the API's reason when it was refused.</summary>
    private async Task<string?> SendFlipAsync(HttpRequestMessage flip)
    {
        using var sent = flip;
        try
        {
            using var _ = await api.SendAsync(sent);
            return null;
        }
        catch (AgentApiException refused) when (IsARefusal(refused.Status))
        {
            return refused.Message;
        }
    }

    /// <summary>A start or a flip the API turned down for a reason it says, as against a key it refused.</summary>
    private static bool IsARefusal(HttpStatusCode status) =>
        status is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict;

    /// <summary>The eval's runs in short, the newest first, as the API sends them.</summary>
    public Task<IReadOnlyList<RunRow>> ListRunsAsync(string eval) =>
        api.GetAsync<IReadOnlyList<RunRow>>(EvalAddress.RunsRouteOf(eval), RunJson.Options);
}

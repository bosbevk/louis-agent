using System.Net;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;

namespace LouisAgent.Web.Evals.Client;

/// <summary>
/// Reads the eval routes for the pages: the host's evals, one run whole, and an eval's runs. It reads with the run's
/// own JSON options, the ones the store wrote it with, strict as every reader of ours.
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

    /// <summary>The eval's runs in short, the newest first, as the API sends them.</summary>
    public Task<IReadOnlyList<RunRow>> ListRunsAsync(string eval) =>
        api.GetAsync<IReadOnlyList<RunRow>>(EvalAddress.RunsRouteOf(eval), RunJson.Options);
}

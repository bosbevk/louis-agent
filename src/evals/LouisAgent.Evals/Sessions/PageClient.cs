using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Sessions;
using LouisAgent.Evals.Runs;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Sessions;

/// <summary>
/// Asks the site's API for the page a conversation names, as the guide's widget would send it, over the same HTTP
/// client the run asks the agent with. A live play asks the page route and keeps the page under its trial, beside
/// the agent's and the judge's answers; a replay reads the kept page back and never asks.
/// </summary>
/// <remarks>
/// The page is kept as a fetch, the store's existing kind for an HTTP answer, under the route's own address, so the
/// engine's kinds stay as they are. A play is a replay by its trial's own <see cref="RunTrial.Replayed" />, as the
/// judge's replay is. Ask for the page after the play's session opens: opening a session that records starts its
/// trial's recording afresh.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0104-a-message-may-carry-the-page.md">ADR 0104: a message may carry the page</seealso>
public sealed class PageClient(HttpClient http, ReplayStore? recordings)
{
    private const string Route = "v1/site/page";

    /// <summary>The page at this address, for this play.</summary>
    public async Task<PageRequest> FindAsync(string address, RunTrial trial,
        CancellationToken cancellationToken = default)
    {
        string at = $"{Route}?address={Uri.EscapeDataString(address)}";
        var ask = ReplayAsk.Of(ReplayKind.Fetch, at);
        if (trial.Replayed is { } recorded) return await ReplayAsync(recorded, ask, address, cancellationToken);

        var page = await AskAsync(at, address, cancellationToken);
        if (recordings is not null) await recordings.SaveAnswerAsync(trial.Name, ask, page, cancellationToken);
        return page;
    }

    /// <summary>The page the recorded play kept; none kept means the recording is out of date.</summary>
    private async Task<PageRequest> ReplayAsync(string recorded, ReplayAsk ask, string address,
        CancellationToken cancellationToken)
    {
        var kept = recordings is null
            ? null
            : await recordings.FindAsync<PageRequest>(recorded, ask, cancellationToken);
        if (kept is ReplayAnswer<PageRequest>.Answered answered) return answered.Value;

        var notRecorded = new PageNotRecordedHoles(address);
        throw new RecordingOutOfDateException(MessageText.Format(SessionMessage.PageNotRecorded, notRecorded));
    }

    /// <summary>Asks the page route; a page the site doesn't have is missing, any other failure is the API's.</summary>
    private async Task<PageRequest> AskAsync(string at, string address, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(at, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new PageMissingException(address);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PageRequest>(JsonSerializerOptions.Web, cancellationToken))!;
    }
}

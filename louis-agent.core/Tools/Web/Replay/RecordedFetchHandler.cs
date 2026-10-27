using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Replay.Answers;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// The fetcher's handler, recorded or replayed (F18 step 4b), by <see cref="ReplayTrial.Current"/> on each request. It sits outside the SSRF
/// guard, so a recording's request still meets the connect-time check. No trial passes through. Record sends the request, buffers the body
/// (up to <see cref="PageFetcher.MaxResponseBytes"/>), saves the <see cref="ReplayResponse"/> under the method and address, and returns
/// the response, whose buffered content reads again; each redirect hop arrives as its own request, so each is its own recording. Only an
/// answer is recorded: a network error, a timeout, a body over the limit and a connect-time refusal propagate unrecorded, and in a replay
/// that request is a miss. Replay never calls the inner handler and answers a miss with <see cref="WebToolMessage.ReplayMiss"/> (ADR 0005).
/// </summary>
public sealed class RecordedFetchHandler(ReplayStore store) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (ReplayTrial.Current is not { } trial) return await base.SendAsync(request, cancellationToken);

        var ask = ReplayAsk.Of(ReplayKind.Fetch,
            new { Method = request.Method.Method, Url = request.RequestUri!.AbsoluteUri });
        return trial.Replaying
            ? await ReplayAsync(trial.Of, ask, request, cancellationToken)
            : await RecordAsync(trial.Of, ask, request, cancellationToken);
    }

    private async Task<HttpResponseMessage> RecordAsync(string trial, ReplayAsk ask, HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        try
        {
            await response.Content.LoadIntoBufferAsync(PageFetcher.MaxResponseBytes, cancellationToken);
            await store.SaveAnswerAsync(trial, ask, await ReplayResponse.OfAsync(response, cancellationToken),
                cancellationToken);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task<HttpResponseMessage> ReplayAsync(string trial, ReplayAsk ask, HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (await store.FindAsync<ReplayResponse>(trial, ask, cancellationToken) is not { } recorded)
            throw new HttpRequestException(MessageText.Format(WebToolMessage.ReplayMiss,
                new { Url = request.RequestUri!.AbsoluteUri }));
        return recorded.OrThrow(error => new HttpRequestException(error)).ToResponse(request);
    }
}

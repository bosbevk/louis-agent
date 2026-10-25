using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Replay.Answers;
using LouisAgent.Core.Tools.Web.Replay.Store;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// The guarded browser, recorded or replayed (F18 step 4a), by the session's <see cref="SessionReplay.Current"/> on each call:
/// none or Off passes through and saves nothing; Record saves the visit under <c>Of</c> and returns it unchanged, a
/// <see cref="WebBrowserException"/> as its text (and rethrown), a cancel not at all; Replay never calls the inner browser, answers
/// from the first recording of the ask, throws a recorded failure's own text, and a miss as a text that names the URL (ADR 0005).
/// Nothing here changes what the inner browser sends or reports (FX21).
/// </summary>
public sealed class RecordedWebBrowser(IWebBrowser inner, WebReplayStore store) : IWebBrowser
{
    public async Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
        string? readySelector = null, CancellationToken cancellationToken = default)
    {
        if (ReplayTrial.Current is not { } trial)
            return await inner.VisitAsync(url, capture, session, script, readySelector, cancellationToken);

        var ask = ReplayAsk.Of(ReplayKind.Visit, new { Url = url.AbsoluteUri, Script = script, ReadySelector = readySelector });
        return trial.Replaying
            ? await ReplayAsync(trial.Of, ask, url, cancellationToken)
            : await RecordAsync(trial.Of, ask, () => inner.VisitAsync(url, capture, session, script, readySelector, cancellationToken), cancellationToken);
    }

    private async Task<BrowserVisit> RecordAsync(string trial, ReplayAsk ask, Func<Task<BrowserVisit>> visitInner, CancellationToken cancellationToken)
    {
        BrowserVisit visit;
        try
        {
            visit = await visitInner();
        }
        catch (WebBrowserException failure)
        {
            await store.SaveFailureAsync(trial, ask, failure.Message, cancellationToken);
            throw;
        }
        await store.SaveAnswerAsync(trial, ask, ReplayPage.Of(visit), cancellationToken);
        return visit;
    }

    private async Task<BrowserVisit> ReplayAsync(string trial, ReplayAsk ask, Uri url, CancellationToken cancellationToken)
    {
        if (await store.FindAsync<ReplayPage>(trial, ask, cancellationToken) is not { } recorded)
            throw new WebBrowserException(MessageText.Format(WebToolMessage.ReplayMiss, new { Url = url.AbsoluteUri }));
        return recorded.OrThrow(error => new WebBrowserException(error)).ToVisit();
    }
}

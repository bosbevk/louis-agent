using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tools.Web.Replay.Store;

/// <summary>
/// What a recording holds for an ask: the page it read (<see cref="Answered"/>) or the failure's text it showed the agent
/// (<see cref="Failed"/>), never both and never neither. <see cref="ToVisit"/> is the replay's answer: the visit, or the same
/// <see cref="WebBrowserException"/> the agent saw.
/// </summary>
public abstract record ReplayedVisit
{
    private ReplayedVisit() { }

    /// <summary>The visit the recording answered with; throws the recorded failure when the recording is one.</summary>
    public abstract BrowserVisit ToVisit();

    public sealed record Answered(ReplayPage Page) : ReplayedVisit
    {
        public override BrowserVisit ToVisit() => Page.ToVisit();
    }

    public sealed record Failed(string Error) : ReplayedVisit
    {
        public override BrowserVisit ToVisit() => throw new WebBrowserException(Error);
    }
}

using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tools.Web.Replay.Answers;

/// <summary>
/// What a visit answered, as its answer file holds it (<c>answers/{sha256}.json</c>): its address after any redirect, its HTML, the
/// captured bodies, the script's result and the cookies the site set. Its hash is the SHA-256 of exactly the JSON bytes of this record.
/// </summary>
public sealed record ReplayPage(string Url, string Html, IReadOnlyList<string> Captured, string? ScriptResult, IReadOnlyList<BrowserCookie>? Cookies)
{
    public static ReplayPage Of(BrowserVisit visit) => new(visit.Url, visit.Html, visit.Captured, visit.ScriptResult, visit.Cookies);

    public BrowserVisit ToVisit() => new(Url, Html, Captured, ScriptResult, Cookies);
}

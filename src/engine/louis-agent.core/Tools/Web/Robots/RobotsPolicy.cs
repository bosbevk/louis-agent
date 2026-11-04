using System.Collections.Concurrent;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Robots.Rules;
using LouisAgent.Core.Tools.Web.Robots.Signals;
using LouisAgent.Choices;

namespace LouisAgent.Core.Tools.Web.Robots;

/// <summary>
/// The one place that answers "may we read this address?" for every fetch and every page the browser opens. It reads
/// a site's robots.txt once a day, remembers it, and checks each address against it; a file it couldn't read counts
/// for an hour, and then it tries again. It also looks at what a page says about itself once it has answered.
/// </summary>
/// <remarks>
/// A policy object checked where every request passes (the fetcher's every hop, the browser's every visit), so no
/// prompt has to remember which sites say no. One <see cref="Lazy{T}"/> task per site in a concurrent dictionary, so
/// checks that arrive together share one fetch. The clock is a <see cref="TimeProvider"/> so a test moves a day in
/// one line (ADR 0046). The fetch of the file is a function handed in: the policy never fetches through a fetcher
/// that has a policy, which is why checking robots.txt itself would loop and is never done.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc9309">RFC 9309: the Robots Exclusion Protocol</seealso>
public sealed class RobotsPolicy(Func<Uri, CancellationToken, Task<RobotsFile>> fetchRobots, TimeProvider? clock = null)
{
    private static readonly TimeSpan ReadFor = TimeSpan.FromDays(1);
    private static readonly TimeSpan UnreachableFor = TimeSpan.FromHours(1);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, Lazy<Task<Entry>>> _entries = new();

    /// <summary>What the site's robots.txt says about this address.</summary>
    public async Task<RobotsVerdict> CheckAsync(Uri url, CancellationToken cancellationToken = default)
    {
        if (IsRobotsTxt(url)) return RobotsVerdict.Allowed;

        var entry = await EntryAsync(url, cancellationToken);
        return entry.VerdictFor(url.PathAndQuery);
    }

    /// <summary>
    /// The text to give the model when this address may not be read, or null when it may.
    /// </summary>
    public async Task<string?> RefusalAsync(Uri url, CancellationToken cancellationToken = default)
    {
        if (IsRobotsTxt(url)) return null;

        var entry = await EntryAsync(url, cancellationToken);
        return entry.VerdictFor(url.PathAndQuery) switch
        {
            RobotsVerdict.Disallowed => MessageText.Format(WebToolMessage.RobotsDisallows,
                new
                {
                    url.Host,
                    Path = url.PathAndQuery
                }),
            RobotsVerdict.Unreachable => MessageText.Format(WebToolMessage.RobotsUnreachable,
                new
                {
                    url.Host,
                    entry.File.Reason
                }),
            _ => null,
        };
    }

    /// <summary>
    /// The text to give the model when the page itself says no to AI readers (a header or a meta tag), or null.
    /// </summary>
    public string? PageRefusal(Uri url, IReadOnlyDictionary<string, string>? headers, string? html) =>
        PageSignals.Read(headers ?? new Dictionary<string, string>(), html) is { } signal
            ? MessageText.Format(WebToolMessage.PageRefusesAiReaders, new
            {
                url.Host,
                Signal = signal.Wire()
            })
            : null;

    /// <summary>Whether this is the robots.txt itself, which is always read and never checked.</summary>
    private static bool IsRobotsTxt(Uri url) => url.AbsolutePath.Equals("/robots.txt", StringComparison.Ordinal);

    /// <summary>
    /// The site's entry from the cache, read again when it has gone stale. A cancelled caller stops waiting, but the
    /// fetch carries on for whoever else is waiting on it.
    /// </summary>
    private async Task<Entry> EntryAsync(Uri url, CancellationToken cancellationToken)
    {
        string site = url.GetLeftPart(UriPartial.Authority);
        var cached = _entries.GetOrAdd(site, _ => Reading(site));
        var entry = await cached.Value.WaitAsync(cancellationToken);
        if (!entry.IsStale(_clock.GetUtcNow())) return entry;

        // Stale: put a new reading in its place. If another check swapped one in first, use theirs.
        _entries.TryUpdate(site, Reading(site), cached);
        return await _entries[site].Value.WaitAsync(cancellationToken);
    }

    /// <summary>A reading of the site's robots.txt that starts when first asked for.</summary>
    private Lazy<Task<Entry>> Reading(string site) => new(async () =>
    {
        var file = await fetchRobots(new Uri(site + "/robots.txt"), CancellationToken.None);
        return Entry.Of(file, _clock.GetUtcNow());
    });

    /// <summary>
    /// One site's robots.txt as read: the file, the rules from it (none when it couldn't be read) and when.
    /// </summary>
    private sealed record Entry(RobotsFile File, RobotsRules? Rules, DateTimeOffset ReadAt)
    {
        /// <summary>
        /// The entry for a file: a good answer is parsed, a 4xx allows all, anything else is unreachable.
        /// </summary>
        public static Entry Of(RobotsFile file, DateTimeOffset now) => file switch
        {
            { Status: >= 200 and < 300 } => new Entry(file, RobotsTxt.Parse(file.Text ?? "", RobotsTxt.UserAgentToken),
                now),
            { Status: >= 400 and < 500 } => new Entry(file, RobotsRules.None, now),
            _ => new Entry(file, null, now),
        };

        /// <summary>Whether it is time to read the file again: an hour for one we couldn't read, else a day.</summary>
        public bool IsStale(DateTimeOffset now) => now - ReadAt >= (Rules is null ? UnreachableFor : ReadFor);

        /// <summary>What this site's rules say about the address.</summary>
        public RobotsVerdict VerdictFor(string pathAndQuery) => Rules switch
        {
            null => RobotsVerdict.Unreachable,
            var rules when rules.Allows(pathAndQuery) => RobotsVerdict.Allowed,
            _ => RobotsVerdict.Disallowed,
        };
    }
}

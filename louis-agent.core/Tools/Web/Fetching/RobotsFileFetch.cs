using LouisAgent.Core.Guardrails.Network;
using LouisAgent.Core.Tools.Web.Robots;

namespace LouisAgent.Core.Tools.Web.Fetching;

/// <summary>
/// Fetches a site's robots.txt for the policy through a page fetcher that has no policy of its own, so the file is
/// always read and the check can't loop. A good answer comes back with its text, a bad status with just the status,
/// and a network failure as no answer at all; nothing here throws for the policy to catch.
/// </summary>
/// <remarks>
/// The fetcher is the process's recording one, so a robots.txt is saved in a recording and a replay reads it from
/// there; one the recording never saw is a miss, which comes back here as no answer and refuses the site, loudly
/// (ADR 0049). It sits in Fetching because it needs the fetcher, and the policy in Robots needs nothing from Fetching.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
public static class RobotsFileFetch
{
    /// <summary>The fetch the policy is built with, reading robots.txt through this fetcher.</summary>
    public static Func<Uri, CancellationToken, Task<RobotsFile>> Through(PageFetcher fetcher) =>
        (url, cancellationToken) => ReadAsync(fetcher, url, cancellationToken);

    /// <summary>The file at this address as the policy wants it.</summary>
    private static async Task<RobotsFile> ReadAsync(PageFetcher fetcher, Uri url, CancellationToken cancellationToken)
    {
        try
        {
            var (response, _) = await fetcher.GetFollowingRedirectsAsync(url);
            using (response)
            {
                int status = (int)response.StatusCode;
                // Only a good answer has text worth reading; any other status is all the policy needs to know.
                return response.IsSuccessStatusCode
                    ? new RobotsFile(status, await response.Content.ReadAsStringAsync(cancellationToken))
                    : new RobotsFile(status, null);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or BlockedAddressException)
        {
            // No answer at all: the policy treats the site as unreachable for an hour.
            return new RobotsFile(null, null);
        }
    }
}

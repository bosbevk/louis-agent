namespace LouisAgent.Composition.Web.Fetching;

/// <summary>Fetches a web page for a tool, with the engine's guards around every request.</summary>
/// <remarks>
/// Separated Interface: the engine holds the guards (no internal addresses, robots.txt on every fetch, recordings for
/// replay), so a pack gets them by asking for a fetcher and can't leave them out.
/// </remarks>
public interface IPageFetcher
{
    /// <summary>The most a page may weigh.</summary>
    const long MaxResponseBytes = 5 * 1024 * 1024;

    /// <summary>Refuses an internal address before anything is sent, to the page or to a service that renders it.</summary>
    Task EnsurePublicAsync(Uri uri);

    /// <summary>Gets the page, checking every redirect, and says where it ended up.</summary>
    Task<(HttpResponseMessage Response, Uri FinalUri)> GetFollowingRedirectsAsync(Uri uri,
        CancellationToken cancellationToken = default);
}

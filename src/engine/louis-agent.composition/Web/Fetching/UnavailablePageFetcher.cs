namespace LouisAgent.Composition.Web.Fetching;

/// <summary>A fetcher on a host with no web access: every fetch fails with the reason, so the tool says why.</summary>
/// <remarks>Null Object, as <c>UnavailablePageRenderer</c> is for the renderer.</remarks>
public sealed class UnavailablePageFetcher(string reason) : IPageFetcher
{
    /// <summary>Fails with the reason: there is nothing to check an address against.</summary>
    public Task EnsurePublicAsync(Uri uri) => throw new BlockedAddressException(reason);

    /// <summary>Fails with the reason: nothing is fetched.</summary>
    public Task<(HttpResponseMessage Response, Uri FinalUri)> GetFollowingRedirectsAsync(Uri uri,
        CancellationToken cancellationToken = default) => throw new BlockedAddressException(reason);
}

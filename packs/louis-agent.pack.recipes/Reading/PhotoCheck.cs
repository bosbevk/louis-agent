using LouisAgent.Composition.Web.Fetching;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Finds the first photo that really loads, so the user never gets a broken image. A page's data can point at a server
/// that's gone (The Food Fox's old host timed out) or one that refuses (its old WordPress address answers 403).
/// </summary>
/// <remarks>
/// The fetch goes through the engine's guarded fetcher, so robots.txt and the address rules hold for a photo as for a
/// page (ADR 0068). A short limit, because a dead server must not slow the answer. A photo that can't be checked for
/// any reason counts as not loading: the recipe matters more than its picture, so this never fails the read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal sealed class PhotoCheck(IPageFetcher fetcher)
{
    /// <summary>How long one photo may take to answer.</summary>
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(3);

    /// <summary>The first of these that loads, or null when none does.</summary>
    public async Task<Uri?> FirstThatLoadsAsync(IEnumerable<Uri> photos, CancellationToken cancellationToken)
    {
        foreach (var photo in photos)
            if (await LoadsAsync(photo, cancellationToken))
                return photo;

        return null;
    }

    /// <summary>A photo loads when it answers OK with an image: a login page answering OK is not one.</summary>
    private async Task<bool> LoadsAsync(Uri photo, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Limit);
        try
        {
            var (response, _) = await fetcher.GetFollowingRedirectsAsync(photo, limit.Token);
            using (response)
                return response.IsSuccessStatusCode &&
                       response.Content.Headers.ContentType?.MediaType?.StartsWith("image/",
                           StringComparison.OrdinalIgnoreCase) == true;
        }
        // Whatever went wrong, it's no photo; only the caller's own cancel still travels.
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}

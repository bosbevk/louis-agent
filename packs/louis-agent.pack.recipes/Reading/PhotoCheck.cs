using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Finds the first photo that isn't known to be gone, so the user doesn't get a broken image. A page's data can point
/// at a server that's gone (The Food Fox's old host timed out) or one that refuses (its old WordPress address answers
/// 403).
/// </summary>
/// <remarks>
/// Only what we know drops a photo: a refusal or not-found, no answer, or something that isn't an image. Food &amp;
/// Home's photos sit on a host whose robots.txt answers 500, so the rules say we may not fetch them (ADR 0068), yet
/// they load fine in the user's browser. So "may not check" and a server error both keep the photo: we fetch nothing
/// we shouldn't, and the user's browser does the loading. An internal address is refused outright and never shown. It
/// never fails the read, because the recipe matters more than its picture.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal sealed class PhotoCheck(IPageFetcher fetcher)
{
    private static readonly ILogger Log = PackLog.For<PhotoCheck>();

    /// <summary>How long one photo may take to answer: a dead server must not slow the answer for long.</summary>
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    /// <summary>The first of these that isn't gone, or null when they all are.</summary>
    public async Task<Uri?> FirstNotGoneAsync(IEnumerable<Uri> photos, CancellationToken cancellationToken)
    {
        foreach (var photo in photos)
        {
            if (!await IsGoneAsync(photo, cancellationToken)) return photo;

            // Only the host in the log, so a dropped photo can be traced without the page's address.
            Log.Log(LogLevel.Information, RecipeLogMessage.PhotoGone, new { photo.Host });
        }

        return null;
    }

    /// <summary>Whether we know the photo won't load; when we can't tell, it isn't gone.</summary>
    private async Task<bool> IsGoneAsync(Uri photo, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Limit);
        try
        {
            // An internal address is refused here, before anything is sent, and that one is gone for good.
            await fetcher.EnsurePublicAsync(photo).WaitAsync(limit.Token);
            return await AnswerSaysGoneAsync(photo, limit.Token);
        }
        // No answer, no such server, or an internal address; only the caller's own cancel still travels.
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return true;
        }
    }

    /// <summary>What the photo's server says. Past the address check, a refusal is the rules, not the photo.</summary>
    private async Task<bool> AnswerSaysGoneAsync(Uri photo, CancellationToken cancellationToken)
    {
        try
        {
            var (response, _) = await fetcher.GetFollowingRedirectsAsync(photo, cancellationToken);
            using (response) return IsGone(response);
        }
        catch (BlockedAddressException)
        {
            return false;
        }
    }

    /// <summary>A server error is the server's trouble, so it stays; a refusal, or OK with no image, is gone.</summary>
    /// <remarks>Guard clauses, one per case (ADR 0051).</remarks>
    private static bool IsGone(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 500) return false;
        if (!response.IsSuccessStatusCode) return true;

        return response.Content.Headers.ContentType?.MediaType?.StartsWith("image/",
            StringComparison.OrdinalIgnoreCase) != true;
    }
}

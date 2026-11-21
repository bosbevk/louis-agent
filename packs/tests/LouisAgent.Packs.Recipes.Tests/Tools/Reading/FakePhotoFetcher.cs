using System.Net;
using LouisAgent.Composition.Web.Fetching;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// A fetcher for the tests that says whether a photo loads: every address is a real image unless the test says what
/// it does instead, and it remembers what it was asked for.
/// </summary>
internal sealed class FakePhotoFetcher(
    Dictionary<string, Func<HttpResponseMessage>>? answers = null,
    string[]? internalAddresses = null) : IPageFetcher
{
    /// <summary>The addresses it was asked to fetch, in order.</summary>
    public List<string> Asked { get; } = [];

    /// <summary>An answer with this status and type.</summary>
    public static Func<HttpResponseMessage> Answers(HttpStatusCode status, string type) => () =>
    {
        var response = new HttpResponseMessage(status) { Content = new ByteArrayContent([1]) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(type);
        return response;
    };

    /// <summary>Refuses the addresses the test calls internal, as the real guard does.</summary>
    public Task EnsurePublicAsync(Uri uri) =>
        internalAddresses?.Contains(uri.AbsoluteUri) == true
            ? Task.FromException(new BlockedAddressException("not a public address"))
            : Task.CompletedTask;

    /// <summary>Answers as the test said, else with a photo.</summary>
    public Task<(HttpResponseMessage Response, Uri FinalUri)> GetFollowingRedirectsAsync(Uri uri,
        CancellationToken cancellationToken = default)
    {
        Asked.Add(uri.AbsoluteUri);
        var answer = answers?.GetValueOrDefault(uri.AbsoluteUri) ?? Answers(HttpStatusCode.OK, "image/jpeg");
        return Task.FromResult((answer(), uri));
    }
}

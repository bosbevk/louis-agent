using System.Net;
using System.Net.Http.Headers;

namespace LouisAgent.Core.Tools.Web.Replay.Answers;

/// <summary>
/// What a fetch answered, as its answer file holds it (<c>answers/{sha256}.json</c>): a short allow list, so nothing else the site sent
/// reaches the folder (no <c>Set-Cookie</c>, no other header) and no request header is saved. The body is base64 in the JSON, because a PDF is
/// binary. <c>FetchUrl</c> reads only the media type, so the allow list keeps its text byte-equal.
/// </summary>
public sealed record ReplayResponse(int Status, string? Reason, string? ContentType, string? Location, byte[] Body)
{
    public static async Task<ReplayResponse> OfAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        new((int)response.StatusCode, response.ReasonPhrase, response.Content.Headers.ContentType?.ToString(), response.Headers.Location?.OriginalString,
            await response.Content.ReadAsByteArrayAsync(cancellationToken));

    public HttpResponseMessage ToResponse(HttpRequestMessage request)
    {
        var response = new HttpResponseMessage((HttpStatusCode)Status) { RequestMessage = request, ReasonPhrase = Reason, Content = new ByteArrayContent(Body) };
        if (ContentType is not null) response.Content.Headers.TryAddWithoutValidation("Content-Type", ContentType);
        if (Location is not null) response.Headers.Location = new Uri(Location, UriKind.RelativeOrAbsolute);
        return response;
    }
}

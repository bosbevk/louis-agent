using System.Net;
using System.Text.Json;
using PuppeteerSharp;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

/// <summary>
/// A response as PuppeteerSharp hands it to a page's <c>Response</c> event, with only the two things the redirect
/// fix reads: its status and its body task. Everything else says it isn't there.
/// </summary>
internal sealed class FakeResponse(HttpStatusCode status, Task<string> body) : IResponse
{
    /// <summary>How many times the body was asked for.</summary>
    public int BodyReads { get; private set; }

    public HttpStatusCode Status => status;

    /// <summary>The body the library would give: awaited the way its own body task is.</summary>
    public async Task<string> TextAsync()
    {
        BodyReads++;
        return await body;
    }

    public string Url => throw new NotSupportedException();
    public Dictionary<string, string> Headers => throw new NotSupportedException();
    public bool Ok => throw new NotSupportedException();
    public IRequest Request => throw new NotSupportedException();
    public bool FromCache => throw new NotSupportedException();
    public SecurityDetails SecurityDetails => throw new NotSupportedException();
    public bool FromServiceWorker => throw new NotSupportedException();
    public string StatusText => throw new NotSupportedException();
    public RemoteAddress RemoteAddress => throw new NotSupportedException();
    public IFrame Frame => throw new NotSupportedException();
    public ResourceTiming Timing => throw new NotSupportedException();
    public ValueTask<byte[]> BufferAsync() => throw new NotSupportedException();
    public Task<JsonDocument> JsonAsync(JsonDocumentOptions options = default) => throw new NotSupportedException();
    public Task<T> JsonAsync<T>(JsonSerializerOptions? options = null) => throw new NotSupportedException();
    public Task<HttpResponseMessage> AsFetchResponseAsync() => throw new NotSupportedException();
}

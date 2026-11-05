using System.Diagnostics;
using LouisAgent.Core.Tests.TestHttp;
using LouisAgent.Core.Delegation.SubSessions;

namespace LouisAgent.Core.Tests.Delegation.SubSessions;

/// <summary>
/// The parent's half of a sub-session's trace (F52 step 4): a request to a peer carries the W3C <c>traceparent</c> of
/// the span that sent it, so the peer's turn joins the same trace. .NET's <see cref="HttpClient"/> writes it on its
/// own when a span is current; this proves it does for <see cref="PeerHttp"/>, over a real client and a real port.
/// </summary>
public class PeerTraceTests
{
    /// <summary>The test's own root span.</summary>
    private static readonly ActivitySource TestSource = new("louis-agent.core.tests.peer-trace");

    /// <summary>
    /// Under a span, the peer request carries its trace as <c>traceparent</c>, beside the key as before.
    /// </summary>
    [Test]
    public async Task UnderASpan_ThePeerRequest_CarriesItsTrace()
    {
        using var echo = new HeaderEcho();
        using var listener = Listening();
        using var http = new HttpClient();
        var peer = new PeerHttp(http, "the-key");

        string traceId;
        using (var root = TestSource.StartActivity("parent turn")!)
        {
            traceId = root.TraceId.ToHexString();
            using var _ = await peer.SendAsync(peer.Request(HttpMethod.Post, echo.Url, new { message = "hallo" }),
                CancellationToken.None);
        }

        // A W3C traceparent is version 00, the 32-hex trace id, a 16-hex span id, and 01 for sampled.
        var headers = echo.Received.Single();
        string[] parts = headers["traceparent"].Split('-');
        Assert.That(headers["x-api-key"], Is.EqualTo("the-key"));
        Assert.That((parts.Length, parts[0], parts[1], parts[2].Length, parts[3]),
            Is.EqualTo((4, "00", traceId, 16, "01")));
    }

    /// <summary>With no span current, no <c>traceparent</c> is made up.</summary>
    [Test]
    public async Task WithNoSpan_ThereIsNoTraceParent()
    {
        using var echo = new HeaderEcho();
        using var http = new HttpClient();
        var peer = new PeerHttp(http, "the-key");

        using var _ = await peer.SendAsync(peer.Request(HttpMethod.Get, echo.Url, null), CancellationToken.None);

        Assert.That(echo.Received.Single().Keys, Has.No.Member("traceparent"));
    }

    /// <summary>Something listening to every source, as a tracer does, so the spans are made.</summary>
    private static ActivityListener Listening()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}

using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// The portal downloads a file the admin key guards, so a link can't do it: the page asks through the API client, which
/// carries the key, and hands the bytes to the browser.
/// </summary>
public class AgentApiBytesTests
{
    /// <summary>The API client over a stub answering with this status and body.</summary>
    private static AgentApi Api(HttpStatusCode status, byte[] body) =>
        new(
            new HttpClient(
                    new StubHandler(_ => new HttpResponseMessage(status) { Content = new ByteArrayContent(body) }))
            { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());

    /// <summary>The bytes come back as they were sent.</summary>
    [Test]
    public async Task TheBytesComeBackAsTheyWereSent() =>
        Assert.That(
            await Api(HttpStatusCode.OK, [80, 75, 3, 4]).GetBytesAsync("v1/admin/benchmarks/runs/r/labels?grader=g"),
            Is.EqualTo(new byte[] { 80, 75, 3, 4 }));

    /// <summary>An error is an exception, not a file holding the error's text.</summary>
    [Test]
    public void AnErrorIsAnApiException_NotAFileOfTheErrorText() =>
        Assert.That(async () => await Api(HttpStatusCode.NotFound, [1]).GetBytesAsync("v1/x"),
            Throws.InstanceOf<AgentApiException>());
}

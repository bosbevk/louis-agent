using System.Net;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;

namespace LouisAgent.Web.Shared.Tests.Api;

// F18 step 6a.3: the portal downloads a file the admin key guards (the people's labels, a zip), so a link cannot do it: the page asks through the API client, which carries the key, and hands
// the bytes to the browser. Written first: AgentApi could read JSON and text, not bytes.
public class AgentApiBytesTests
{
    private static AgentApi Api(HttpStatusCode status, byte[] body) =>
        new(new HttpClient(new StubHandler(_ => new HttpResponseMessage(status) { Content = new ByteArrayContent(body) })) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());

    [Test]
    public async Task TheBytesComeBackAsTheyWereSent() =>
        Assert.That(await Api(HttpStatusCode.OK, [80, 75, 3, 4]).GetBytesAsync("v1/admin/benchmarks/runs/r/labels?grader=g"), Is.EqualTo(new byte[] { 80, 75, 3, 4 }));

    [Test]
    public void AnErrorIsAnApiException_NotAFileOfTheErrorText() =>
        Assert.That(async () => await Api(HttpStatusCode.NotFound, [1]).GetBytesAsync("v1/x"), Throws.InstanceOf<AgentApiException>());
}

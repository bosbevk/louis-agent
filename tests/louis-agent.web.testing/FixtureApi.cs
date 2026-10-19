using System.Net;
using System.Net.Http.Headers;
using System.Text;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;

namespace LouisAgent.Web.Testing;

/// <summary>
/// A stub of louis-agent.api for a page rendered in a test: fixed JSON for what the apps read at start-up, one session
/// opened on request, the recorded Puls turn streamed for its message, and the code-review app's workspace and git
/// reads answered empty. The API client is the real <see cref="AgentApi"/> over it, so a page's requests go the way
/// they go in a browser, minus the network. No rates: costs show as charged and the currency switcher stays hidden.
/// </summary>
public static class FixtureApi
{
    public const string SessionId = "sess_fixture";

    /// <summary>The client the pages use, on a stub that answers from the fixture.</summary>
    public static AgentApi Client(ChatSettings settings) =>
        new(new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") }, settings);

    public static HttpResponseMessage Answer(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        bool get = request.Method == HttpMethod.Get, post = request.Method == HttpMethod.Post;
        return (path, get, post) switch
        {
            ("/model", true, _) => Json("""{"provider":"anthropic","model":"claude-haiku-5-5"}"""),
            ("/roles", true, _) => Json("""{"default":"louis-agent/voice","agent":["louis-agent/voice","puls/voice-af","puls/voice-en"],"specialist":["puls-cook"],"minion":[]}"""),
            ("/toolsets", true, _) => Json("""{"default":["skills"],"toolsets":[{"name":"files","available":true},{"name":"git","available":true},{"name":"web","available":true},{"name":"specialists","available":true},{"name":"shops","available":true}]}"""),
            ("/sessions", _, true) => Json($$"""{"session_id":"{{SessionId}}","toolsets":["specialists","web"]}"""),
            ($"/sessions/{SessionId}/messages", _, true) => Stream(),
            ("/git/status", true, _) => Json("""{"branch":"main","files":[],"committed":null}"""),
            ("/git/branches", true, _) => Json("""{"current":"main","branches":[],"message":null}"""),
            ("/git/log", true, _) => Json("""{"commits":[]}"""),
            ("/workspace/entries", true, _) => Json("""{"path":"","entries":[]}"""),
            // The code-review app's theme list, as its wwwroot ships it.
            ("/css/themes/themes.json", true, _) => Json(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.web", "wwwroot", "css", "themes", "themes.json"))),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found_error","message":"not in the fixture"}}""", Encoding.UTF8, "application/json"),
            },
        };
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Stream()
    {
        var content = new StreamContent(File.OpenRead(SessionStreamFixture.StreamFile));
        content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}

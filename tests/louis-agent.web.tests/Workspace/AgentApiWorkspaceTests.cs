using System.Net;
using System.Text;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Workspace;

namespace LouisAgent.Web.Tests.Workspace;

// CL20 step 5 (ADR 0039): the code-review app's git calls name the API's resources. Each call is recorded as "METHOD path?query",
// escapes kept (StubHandler.Requests shows them undone).
public class AgentApiWorkspaceTests
{
    private readonly List<string> _calls = [];

    // NUnit builds one instance for the whole fixture, so each test starts from an empty record.
    [SetUp]
    public void SetUp() => _calls.Clear();

    private AgentApi Api() => new(
        new HttpClient(new StubHandler(request =>
        {
            _calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        })) { BaseAddress = new Uri("http://localhost/") },
        new ChatSettings());

    [Test]
    public async Task ACommit_IsAPostToTheCommits()
    {
        await Api().GitCommitAsync("x");

        Assert.That(_calls, Is.EqualTo(new[] { "POST /v1/git/commits" }));
    }

    [Test]
    public async Task TheLog_IsTheCommits()
    {
        await Api().GitLogAsync();

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/commits?count=200" }));
    }

    [Test]
    public async Task ACommit_IsReadByItsSha()
    {
        await Api().GitShowCommitAsync("abc1234");

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/commits/abc1234" }));
    }

    [Test]
    public async Task ABranch_IsReadByItsName_SlashKept_AndTheRestEscaped()
    {
        await Api().GitBranchAsync("feature/#12");

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/branches/feature/%2312" }));
    }

    [Test]
    public async Task AMerge_IsAnAction_APostToMerge()
    {
        await Api().GitMergeAsync("feature/x");

        Assert.That(_calls, Is.EqualTo(new[] { "POST /v1/git/merge" }));
    }

    [Test]
    public async Task ADeletedBranch_IsADeleteOnTheBranch()
    {
        await Api().GitDeleteBranchAsync("feature/x");

        Assert.That(_calls, Is.EqualTo(new[] { "DELETE /v1/git/branches/feature/x" }));
    }
}

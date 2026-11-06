using System.Net;
using System.Text;
using LouisAgent.CodeReview.Workspace;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;

namespace LouisAgent.CodeReview.Tests.Workspace;

/// <summary>
/// The code-review app's git calls name the API's resources (ADR 0039). Each call is recorded as "METHOD path?query",
/// escapes kept (StubHandler.Requests shows them undone).
/// </summary>
public class AgentApiWorkspaceTests
{
    private readonly List<string> _calls = [];

    /// <summary>NUnit builds one instance for the whole fixture, so each test starts from an empty record.</summary>
    [SetUp]
    public void SetUp() => _calls.Clear();

    /// <summary>The API client over a stub that records each call.</summary>
    private AgentApi Api() => new(
        new HttpClient(new StubHandler(request =>
        {
            _calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        }))
        { BaseAddress = new Uri("http://localhost/") },
        new ChatSettings());

    /// <summary>A commit is a post to the commits.</summary>
    [Test]
    public async Task ACommit_IsAPostToTheCommits()
    {
        await Api().GitCommitAsync("x");

        Assert.That(_calls, Is.EqualTo(new[] { "POST /v1/git/commits" }));
    }

    /// <summary>The log is a read of the commits.</summary>
    [Test]
    public async Task TheLog_IsTheCommits()
    {
        await Api().GitLogAsync();

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/commits?count=200" }));
    }

    /// <summary>One commit is read by its sha.</summary>
    [Test]
    public async Task ACommit_IsReadByItsSha()
    {
        await Api().GitShowCommitAsync("abc1234");

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/commits/abc1234" }));
    }

    /// <summary>A branch is read by its name: slashes stay, and the rest is escaped.</summary>
    [Test]
    public async Task ABranch_IsReadByItsName_SlashKept_AndTheRestEscaped()
    {
        await Api().GitBranchAsync("feature/#12");

        Assert.That(_calls, Is.EqualTo(new[] { "GET /v1/git/branches/feature/%2312" }));
    }

    /// <summary>A merge is an action, so it's a post.</summary>
    [Test]
    public async Task AMerge_IsAnAction_APostToMerge()
    {
        await Api().GitMergeAsync("feature/x");

        Assert.That(_calls, Is.EqualTo(new[] { "POST /v1/git/merge" }));
    }

    /// <summary>Deleting a branch is a delete on the branch.</summary>
    [Test]
    public async Task ADeletedBranch_IsADeleteOnTheBranch()
    {
        await Api().GitDeleteBranchAsync("feature/x");

        Assert.That(_calls, Is.EqualTo(new[] { "DELETE /v1/git/branches/feature/x" }));
    }
}

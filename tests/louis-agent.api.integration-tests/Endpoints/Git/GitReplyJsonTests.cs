using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Endpoints.Git;

/// <summary>
/// Pins the JSON the git routes send the code-review app: every object's names, in order, so the app reads them
/// however the endpoints are shaped. Shas, dates and diffs change from run to run, so the names are pinned and the
/// values only where they are fixed.
/// </summary>
[Category("Integration")]
public class GitReplyJsonTests
{
    private const string BranchNames = "name,commit,subject,author,date,current,ahead,behind,merged";
    private const string CommitNames = "sha,shortSha,author,date,subject,refs,parents";

    private TempGitRepo _repo = null!;

    /// <summary>A repository with <c>main</c> and a <c>feature</c> branch one commit ahead of it.</summary>
    [SetUp]
    public void SetUp()
    {
        _repo = new TempGitRepo();
        _repo.Git("checkout", "-q", "-b", "feature");
        _repo.Write("notes.md", "Notes\n");
        _repo.Git("add", "notes.md");
        _repo.Git("commit", "-q", "-m", "Add notes");
        _repo.Git("checkout", "-q", "main");
    }

    /// <summary>The repository goes with the test.</summary>
    [TearDown]
    public void TearDown() => _repo.Dispose();

    /// <summary>The branch list names the checked-out branch, every branch, and a message after a change.</summary>
    [Test]
    public async Task TheBranchList_KeepsItsNames()
    {
        await using var app = await StartAsync();

        using var json = await GetAsync(app, "/v1/git/branches");

        Assert.That(Names(json.RootElement), Is.EqualTo("current,branches,message"));
        Assert.That(json.RootElement.GetProperty("current").GetString(), Is.EqualTo("main"));
        Assert.That(Names(json.RootElement.GetProperty("branches")[0]), Is.EqualTo(BranchNames));
    }

    /// <summary>A branch's page: the branch, what it merges into, its commits and its diff.</summary>
    [Test]
    public async Task ABranchsDiff_KeepsItsNames()
    {
        await using var app = await StartAsync();

        using var json = await GetAsync(app, "/v1/git/branches/feature");

        Assert.That(Names(json.RootElement), Is.EqualTo("branch,into,commits,diff,truncated"));
        Assert.That(Names(json.RootElement.GetProperty("branch")), Is.EqualTo(BranchNames));
        Assert.That(Names(json.RootElement.GetProperty("commits")[0]), Is.EqualTo(CommitNames));
    }

    /// <summary>A merge answers with the branch list again, and a message saying what it did.</summary>
    [Test]
    public async Task AMerge_AnswersTheBranchList_WithAMessage()
    {
        await using var app = await StartAsync();

        using var response = await app.GetTestClient().PostAsJsonAsync("/v1/git/merge", new { branch = "feature" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Names(json.RootElement), Is.EqualTo("current,branches,message"));
        Assert.That(json.RootElement.GetProperty("message").GetString(), Is.Not.Empty);
    }

    /// <summary>Starts the API over the repository.</summary>
    private async Task<WebApplication> StartAsync()
    {
        var app = ApiTestHost.Build(workspaceRoot: _repo.Path);
        await app.StartAsync();
        return app;
    }

    /// <summary>Reads a route that must answer 200.</summary>
    private static async Task<JsonDocument> GetAsync(WebApplication app, string path)
    {
        using var response = await app.GetTestClient().GetAsync(path);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), path);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>The object's names in the order they were written.</summary>
    private static string Names(JsonElement element) =>
        string.Join(",", element.EnumerateObject().Select(p => p.Name));
}

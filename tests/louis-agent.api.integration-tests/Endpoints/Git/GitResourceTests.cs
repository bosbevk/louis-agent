using LouisAgent.Testing;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Endpoints.Git;

// CL20 step 5 (ADR 0039, rules 2 to 4): the git routes name resources (commits and branches), a create answers 201 with its
// Location, and a delete is a DELETE. On a small repository made for the test, through the API in memory. Skipped when git isn't
// installed.
[Category("Integration")]
public class GitResourceTests
{
    private string _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Path.Combine(Path.GetTempPath(), "git-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
        try
        {
            Git("init", "-q", "-b", "main");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Outside.Tool(false, "git isn't installed");
        }

        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        Git("config", "commit.gpgsign", "false");
        File.WriteAllText(Path.Combine(_repo, "readme.md"), "Hallo\n");
        Git("add", "readme.md");
        Git("commit", "-q", "-m", "Begin");
    }

    [TearDown]
    public void TearDown()
    {
        // git makes its object files read-only; clear that so the folder can go.
        foreach (string file in Directory.EnumerateFiles(_repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_repo, recursive: true);
    }

    private string Git(params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = _repo, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)!;
        var error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {error.Result}");
        return output;
    }

    [Test]
    public async Task ACommit_Answers201_WithItsLocation()
    {
        await using var app = ApiTestHost.Build(workspaceRoot: _repo);
        await app.StartAsync();
        var client = app.GetTestClient();
        File.WriteAllText(Path.Combine(_repo, "notes.md"), "Notes\n");
        Git("add", "notes.md");

        using var response = await client.PostAsJsonAsync("/v1/git/commits", new { message = "Add notes" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo($"/v1/git/commits/{Git("rev-parse", "HEAD").Trim()}"));
        using var read = await client.GetAsync(response.Headers.Location);
        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ABranchWithASlash_IsReadThenDeleted()
    {
        await using var app = ApiTestHost.Build(workspaceRoot: _repo);
        await app.StartAsync();
        var client = app.GetTestClient();
        Git("branch", "feature/x");   // on HEAD, so already merged

        using var read = await client.GetAsync("/v1/git/branches/feature/x");
        using var body = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        using var delete = await client.DeleteAsync("/v1/git/branches/feature/x");

        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body.RootElement.GetProperty("branch").GetProperty("name").GetString(), Is.EqualTo("feature/x"));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Git("branch", "--list", "feature/x").Trim(), Is.Empty);
    }

    [Test]
    public async Task MergingTheCheckedOutBranch_IsRefused_InApiUserTextsWords()
    {
        await using var app = ApiTestHost.Build(workspaceRoot: _repo);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/v1/git/merge", new { branch = "main" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body.RootElement.GetProperty("error").GetProperty("message").GetString(),
            Is.EqualTo(MessageText.Format(ApiUserText.MergeCheckedOutBranch, new { Branch = "main" })));
    }
}

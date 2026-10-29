using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Endpoints;

/// <summary>
/// Pins the error JSON of the two kinds of error that need a process to happen: git refusing, and a run started while
/// another one is going. The other kinds are pinned in the API's unit tests.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
[Category("Integration")]
public class ApiErrorRouteTests
{
    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>A fresh folder per test: no git repository, and where the runner works.</summary>
    private string _workspace = "";

    /// <summary>Makes the test's own folder.</summary>
    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-errors-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    /// <summary>Deletes the test's folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_workspace, recursive: true);

    /// <summary>
    /// Asking for the git status of a folder that isn't a repository is git's own refusal, in the API's shape. The
    /// words are git's, so they're read back and only the shape around them is pinned.
    /// </summary>
    [Test]
    public async Task GitRefusing_IsAGitError_ByteForByte()
    {
        Outside.Tool(GitIsInstalled(), "git isn't installed");
        await using var app = ApiTestHost.Build(workspaceRoot: _workspace);
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync("/v1/git/status");
        string body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        string message = json.RootElement.GetProperty("error").GetProperty("message").GetString()!;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body, Is.EqualTo(
            $$$"""{"type":"error","error":{"type":"git_error","message":{{{JsonSerializer.Serialize(message)}}}}}"""));
    }

    /// <summary>Starting a run while another one is going is a conflict that names the run that's going.</summary>
    [Test]
    public async Task StartingARun_WhileOneRuns_IsAConflict_ByteForByte()
    {
        // The real runner with only the stub, and a store with its recorder so a start gets as far as the runner.
        var store = new InMemoryEvalStore();
        var catalog = new EvalList([new EvalEntry("stub", "Stub")]);
        var runner = new EvalProcess(catalog, _workspace, "http://127.0.0.1:1/", RunnerBuild.Dll);
        await using var app = ApiTestHost.Build(workspaceRoot: _workspace, adminKey: AdminKey, store: store,
            runner: runner, recorder: new EvalRecorder(store), catalog: catalog);
        await app.StartAsync();

        // A run with every repeat takes longer to finish than the second start takes to arrive.
        var busy = runner.Start("stub", EvalProcess.MaxRepeat)!;
        // The start takes a JSON body, so the route only matches a request that sends one.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/admin/evals/stub/runs")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", AdminKey);
        using var response = await app.GetTestClient().SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        // Let the busy run finish before the test ends.
        await foreach (string _ in runner.EventsAsync(busy.Id))
        {
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(body, Is.EqualTo(
            """{"type":"error","error":{"type":"conflict_error","message":"A run of stub is in progress; wait for it to end. The containers are shared, so one runs at a time."}}"""));
    }

    /// <summary>We can only ask git if it's on this machine.</summary>
    private static bool GitIsInstalled()
    {
        // Starting it is the only sure test; a missing program throws.
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            })!;
            git.WaitForExit();
            return git.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}

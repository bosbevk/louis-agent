using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Endpoints;

/// <summary>
/// Pins the error JSON of the kind of error that needs a process to happen: git refusing. The other kinds are pinned
/// in the API's unit tests.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
[Category("Integration")]
public class ApiErrorRouteTests
{
    /// <summary>A fresh folder per test: no git repository.</summary>
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

using System.Net;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the error JSON, byte for byte, of one real route per kind of error the API sends, so the web apps and the
/// orchestrator keep reading what they read today. The git failure and the conflict need a process, so they are pinned
/// in the integration tests.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class ApiErrorRouteTests
{
    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>A workspace of its own, holding a secret file, a file too big to show and a binary one.</summary>
    private string _workspace = "";

    /// <summary>Makes the workspace and the three files the refusals need.</summary>
    [OneTimeSetUp]
    public void MakeWorkspace()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-errors-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);

        File.WriteAllText(Path.Combine(_workspace, ".env"), "SECRET=1");
        File.WriteAllText(Path.Combine(_workspace, "big.txt"), new string('a', 1_000_001));
        File.WriteAllBytes(Path.Combine(_workspace, "bin.dat"), [(byte)'a', 0, (byte)'b']);
    }

    /// <summary>Deletes the workspace.</summary>
    [OneTimeTearDown]
    public void DeleteWorkspace() => Directory.Delete(_workspace, recursive: true);

    /// <summary>
    /// One route per kind: the path, whether it sends the admin key, and the status and body it answers today.
    /// </summary>
    private static IEnumerable<TestCaseData> Routes()
    {
        yield return new TestCaseData("/v1/workspace/file", false, HttpStatusCode.BadRequest,
                """{"type":"error","error":{"type":"invalid_request_error","message":"Pass the file's workspace-relative 'path'."}}""")
            .SetName("BadRequest");
        yield return new TestCaseData("/v1/admin/evals/runs/x", false, HttpStatusCode.Unauthorized,
                """{"type":"error","error":{"type":"authentication_error","message":"Missing or invalid API key (x-api-key or Authorization: Bearer)."}}""")
            .SetName("Unauthorized");
        yield return new TestCaseData("/v1/workspace/file?path=.env", false, HttpStatusCode.Forbidden,
                """{"type":"error","error":{"type":"permission_error","message":"Secret files (.env, keys, certificates) can't be opened."}}""")
            .SetName("Forbidden");
        yield return new TestCaseData("/v1/workspace/file?path=gone.txt", false, HttpStatusCode.NotFound,
                """{"type":"error","error":{"type":"not_found_error","message":"No file at 'gone.txt'."}}""")
            .SetName("NotFound");
        yield return new TestCaseData("/v1/workspace/file?path=big.txt", false, HttpStatusCode.RequestEntityTooLarge,
                """{"type":"error","error":{"type":"request_too_large","message":"big.txt is larger than 1 MB."}}""")
            .SetName("TooLarge");
        yield return new TestCaseData("/v1/workspace/file?path=bin.dat", false, HttpStatusCode.UnsupportedMediaType,
                """{"type":"error","error":{"type":"invalid_request_error","message":"bin.dat is a binary file."}}""")
            .SetName("UnsupportedMediaType");
        yield return new TestCaseData("/v1/admin/evals/runs/x", true, HttpStatusCode.ServiceUnavailable,
                """{"type":"error","error":{"type":"api_error","message":"The eval store isn't set, so a run's results would be lost: set EVALS_MONGO_URL in config/.env.secrets and recreate the container."}}""")
            .SetName("Unavailable");
    }

    /// <summary>Each route answers its status, and its error exactly as the clients read it today.</summary>
    [TestCaseSource(nameof(Routes))]
    public async Task TheRoute_AnswersItsError_ByteForByte(string path, bool withKey, HttpStatusCode status,
        string body)
    {
        await using var app = ApiTestHost.Build(workspaceRoot: _workspace, adminKey: AdminKey);
        await app.StartAsync();

        // The admin routes need the key to get past the gate; without it the gate answers.
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (withKey) request.Headers.Add("x-api-key", AdminKey);
        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(status));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(body));
    }
}

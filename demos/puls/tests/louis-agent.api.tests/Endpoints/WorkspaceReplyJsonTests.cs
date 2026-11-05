using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the JSON the code-review app reads when it browses the workspace: a folder's entries and a file, byte for
/// byte, and the words of a file too big or not text. Over a small folder made for the test.
/// </summary>
public class WorkspaceReplyJsonTests
{
    private string _root = null!;

    /// <summary>A workspace with a folder and a small text file.</summary>
    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "workspace-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "docs"));
        File.WriteAllText(Path.Combine(_root, "notes.txt"), "Hallo");
    }

    /// <summary>The workspace goes with the test.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    /// <summary>
    /// A folder's entries: folders first, then files, each with its path, type, size and whether it's secret.
    /// </summary>
    [Test]
    public async Task AFoldersEntries_AreWrittenAsBefore()
    {
        Assert.That(await BodyAsync("/v1/workspace/entries"), Is.EqualTo(
            """{"path":"","entries":[{"name":"docs","path":"docs","type":"directory","size":null,"sensitive":false},{"name":"notes.txt","path":"notes.txt","type":"file","size":5,"sensitive":false}]}"""));
    }

    /// <summary>A file: its path, name, size and text.</summary>
    [Test]
    public async Task AFile_IsWrittenAsBefore()
    {
        Assert.That(await BodyAsync("/v1/workspace/file?path=notes.txt"),
            Is.EqualTo("""{"path":"notes.txt","name":"notes.txt","size":5,"content":"Hallo"}"""));
    }

    /// <summary>A file over a megabyte, and a file that isn't text, say so in their own words.</summary>
    [Test]
    public async Task ABigOrBinaryFile_SaysWhy()
    {
        File.WriteAllBytes(Path.Combine(_root, "big.txt"), new byte[1_000_001]);
        File.WriteAllBytes(Path.Combine(_root, "data.bin"), [1, 0, 2]);

        string big = await BodyAsync("/v1/workspace/file?path=big.txt", HttpStatusCode.RequestEntityTooLarge);
        string binary = await BodyAsync("/v1/workspace/file?path=data.bin", HttpStatusCode.UnsupportedMediaType);

        Assert.That(big, Does.Contain("\"message\":\"big.txt is larger than 1 MB.\""));
        Assert.That(binary, Does.Contain("\"message\":\"data.bin is a binary file.\""));
    }

    /// <summary>Asks the API over the workspace and hands back the body of the answer with this status.</summary>
    private async Task<string> BodyAsync(string path, HttpStatusCode status = HttpStatusCode.OK)
    {
        await using WebApplication app = ApiTestHost.Build(workspaceRoot: _root);
        await app.StartAsync();

        using var response = await app.GetTestClient().GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(status), path);
        return await response.Content.ReadAsStringAsync();
    }
}

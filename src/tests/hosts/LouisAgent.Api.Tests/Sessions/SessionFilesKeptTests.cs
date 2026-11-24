using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Testing;
using LouisAgent.Choices;
using LouisAgent.Composition.Packs.Files;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// F171.1 step 5: a session keeps the photos and PDFs sent with its messages, by name, for a tool to read. A file of
/// a kind a model can't read, one over 10 MB, or one that would take the session's files past 20 MB is refused with a
/// 400 that says which check, and a text attachment is kept as it always was.
/// </summary>
public class SessionFilesKeptTests
{
    /// <summary>A megabyte, as the limits count one.</summary>
    private const int Megabyte = 1024 * 1024;

    /// <summary>A model that answers in a few words.</summary>
    private static FakeChatClient Answers() =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Got it.")));

    /// <summary>A photo or a PDF of this many bytes, as a message carries one.</summary>
    private static AttachmentRequest File(string name, int bytes, string mediaType = "image/jpeg") =>
        new(name, null, new byte[bytes], mediaType);

    /// <summary>An API with one open session, and its id.</summary>
    private static async Task<(WebApplication App, HttpClient Client, string Id)> OpenAsync()
    {
        var app = ApiTestHost.Build(model: Answers());
        await app.StartAsync();
        var client = app.GetTestClient();

        var opening = new CreateSessionRequest(null, null, null, [TestToolset.Files.Wire()]);
        using var opened = await client.PostAsJsonAsync("/v1/sessions", opening);
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return (app, client, body.RootElement.GetProperty("session_id").GetString()!);
    }

    /// <summary>Sends a message with these attachments, and reads its answer to the end.</summary>
    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string id,
        params AttachmentRequest[] attachments)
    {
        var message = new SendMessageRequest("Check this", [.. attachments]);
        var answer = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages", message);
        await answer.Content.ReadAsStringAsync();
        return answer;
    }

    /// <summary>The session the API holds under this id.</summary>
    private static AgentSession Session(WebApplication app, string id) =>
        app.Services.GetRequiredService<AgentSessions>().Get(id)!;

    /// <summary>A JPEG of 1 MB is kept, by its name, its kind and its size.</summary>
    [Test]
    public async Task AJpegOfOneMegabyte_IsKept_ByItsNameKindAndSize()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var answer = await SendAsync(client, id, File("account.jpg", Megabyte));
        var kept = Session(app, id).Files.Find("account.jpg");

        Assert.Multiple(() =>
        {
            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(kept?.MediaType, Is.EqualTo(FileMediaType.Jpeg));
            Assert.That(kept?.Bytes.Length, Is.EqualTo(Megabyte));
        });
    }

    /// <summary>The same name again replaces the file it had.</summary>
    [Test]
    public async Task TheSameNameAgain_ReplacesTheFile()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var first = await SendAsync(client, id, File("account.pdf", Megabyte, "application/pdf"));
        using var second = await SendAsync(client, id, File("account.pdf", 2 * Megabyte, "application/pdf"));

        Assert.That(Session(app, id).Files.Find("account.pdf")?.Bytes.Length, Is.EqualTo(2 * Megabyte));
    }

    /// <summary>A kind a model can't read is refused with a 400 that names it, and nothing is kept.</summary>
    [Test]
    public async Task AKindAModelCantRead_IsRefused_NamingTheKind()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var answer = await SendAsync(client, id, File("scan.tiff", Megabyte, "image/tiff"));

        Assert.Multiple(async () =>
        {
            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await answer.Content.ReadAsStringAsync(), Does.Contain("image/tiff"));
            Assert.That(Session(app, id).Files.Find("scan.tiff"), Is.Null);
        });
    }

    /// <summary>A file over 10 MB is refused with a 400 that names the limit.</summary>
    [Test]
    public async Task AFileOverTenMegabytes_IsRefused_NamingTheLimit()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var answer = await SendAsync(client, id, File("big.jpg", 10 * Megabyte + 1));

        Assert.Multiple(async () =>
        {
            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await answer.Content.ReadAsStringAsync(), Does.Contain("10 MB"));
            Assert.That(Session(app, id).Files.Find("big.jpg"), Is.Null);
        });
    }

    /// <summary>
    /// A file that would take the session's files past 20 MB is refused with a 400 that names that limit, and the
    /// files it kept before stay.
    /// </summary>
    [Test]
    public async Task AFileThatWouldTakeTheSessionPastTwentyMegabytes_IsRefused_NamingThatLimit()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var first = await SendAsync(client, id, File("one.jpg", 9 * Megabyte), File("two.jpg", 9 * Megabyte));
        using var answer = await SendAsync(client, id, File("three.jpg", 3 * Megabyte));

        Assert.Multiple(async () =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await answer.Content.ReadAsStringAsync(), Does.Contain("20 MB"));
            Assert.That(Session(app, id).Files.Find("three.jpg"), Is.Null);
            Assert.That(Session(app, id).Files.Find("one.jpg"), Is.Not.Null);
        });
    }

    /// <summary>A text attachment is kept as it always was, as text, and is no file.</summary>
    [Test]
    public async Task ATextAttachment_IsKeptAsItAlwaysWas()
    {
        var (app, client, id) = await OpenAsync();
        await using var _ = app;

        using var answer = await SendAsync(client, id, new AttachmentRequest("list.txt", "eggs"));

        Assert.Multiple(() =>
        {
            Assert.That(Session(app, id).Attachments["list.txt"], Is.EqualTo("eggs"));
            Assert.That(Session(app, id).Files.Find("list.txt"), Is.Null);
        });
    }
}

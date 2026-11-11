using System.Text.Json;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.SessionStream.Tests.Wire.Requests;

/// <summary>
/// Pins what the session requests write (F127.3.1 step 1), byte for byte, with .NET's web defaults: what
/// <c>JsonContent.Create</c> sends and what the API reads. Core's sub-sessions and the browser write these records now,
/// in place of their anonymous objects and the browser's own body, so the names are the ones those sent: camelCase,
/// <c>time_zone</c> spelled out, the nulls written.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public class SessionRequestJsonTests
{
    /// <summary>A sub-session's opening on the wire: every field, in the record's order.</summary>
    private static readonly string SubSessionJson = Compact(
        """
        {
          "task": "sub-session:alpha", "run": "run-1", "service": "svc", "toolsets": ["alpha"], "depth": 1,
          "parent": "turn-1", "location": "Cape Town", "time_zone": "Africa/Johannesburg", "llm": null,
          "tool": null, "role": "alpha-cook", "stream": null,
          "replay": { "web": "record", "of": "run-1/task-1", "model": false }, "identity": null
        }
        """);

    /// <summary>The browser's opening on the wire: its own fields, the rest null.</summary>
    private static readonly string BrowserJson = Compact(
        """
        {
          "task": null, "run": null, "service": null, "toolsets": ["alpha"], "depth": 0, "parent": null,
          "location": null, "time_zone": "Africa/Johannesburg", "llm": null, "tool": null, "role": null,
          "stream": null, "replay": null, "identity": "alpha-guide"
        }
        """);

    /// <summary>A message on the wire, with one attachment and no page.</summary>
    private static readonly string MessageJson = Compact(
        """
        {
          "message": "Plan three dinners", "attachments": [{ "name": "list.txt", "content": "eggs" }],
          "page": null
        }
        """);

    /// <summary>A page on the wire, with one area that is also the one in view.</summary>
    private static readonly string PageJson = Compact(
        """
        {
          "url": "/home", "title": "Home", "text": "Words", "areas": [{ "id": "rule", "heading": "The rule" }],
          "inView": { "id": "rule", "heading": "The rule" }, "open": null, "links": null
        }
        """);

    /// <summary>
    /// A sub-session's opening: its toolsets, depth, tags, role, the user's place and the parent's trial.
    /// </summary>
    private static CreateSessionRequest SubSession() => new("sub-session:alpha", "run-1", "svc", ["alpha"], Depth: 1,
        Parent: "turn-1", Location: "Cape Town", TimeZone: "Africa/Johannesburg", Role: "alpha-cook",
        Replay: new ReplayRequest("record", "run-1/task-1"));

    /// <summary>Every field, in the record's order, by the name the API reads.</summary>
    [Test]
    public void ASubSessionsOpening_WritesTheNamesTheApiReads() =>
        Assert.That(Written(SubSession()), Is.EqualTo(SubSessionJson));

    /// <summary>The browser's opening: the toolsets, where the user is and who answers; the rest left null.</summary>
    [Test]
    public void ABrowsersOpening_WritesItsFields_TheRestNull() =>
        Assert.That(Written(new CreateSessionRequest(null, null, null, ["alpha"], TimeZone: "Africa/Johannesburg",
            Identity: "alpha-guide")), Is.EqualTo(BrowserJson));

    /// <summary>What a sub-session writes reads back as the same request, as the API reads it.</summary>
    [Test]
    public void AnOpening_ReadsBack_AsItWasWritten()
    {
        string json = Written(SubSession());

        var read = JsonSerializer.Deserialize<CreateSessionRequest>(json, JsonSerializerOptions.Web);

        Assert.That(Written(read), Is.EqualTo(json));
    }

    /// <summary>A message goes with its attachments, each by name and content, and no page.</summary>
    [Test]
    public void AMessage_WritesItsAttachments_AndNoPage() =>
        Assert.That(Written(new SendMessageRequest("Plan three dinners", [new AttachmentRequest("list.txt", "eggs")])),
            Is.EqualTo(MessageJson));

    /// <summary>
    /// A page travels by the names the browser's own page writes: <c>inView</c>, <c>open</c>, <c>links</c>.
    /// </summary>
    [Test]
    public void APage_WritesTheNamesTheBrowserSends() =>
        Assert.That(Written(new PageRequest("/home", "Home", "Words", [new PageAreaRequest("rule", "The rule")],
            InView: new PageAreaRequest("rule", "The rule"))), Is.EqualTo(PageJson));

    /// <summary>A request as it goes on the wire.</summary>
    private static string Written<T>(T request) => JsonSerializer.Serialize(request, JsonSerializerOptions.Web);

    /// <summary>The expected JSON, written readably here, in the wire's compact form with its order kept.</summary>
    private static string Compact(string json) => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);
}

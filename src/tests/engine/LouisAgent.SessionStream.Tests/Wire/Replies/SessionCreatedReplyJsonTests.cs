using System.Text.Json;
using LouisAgent.SessionStream.Wire.Replies;

namespace LouisAgent.SessionStream.Tests.Wire.Replies;

/// <summary>
/// A session's opening reply, as every client reads it since F127.3.1 step 3: what the API writes with
/// <see cref="SessionJson.SnakeCase"/> reads back whole, and a reply with only an id (an older or smaller peer) reads
/// with the rest left empty, so a reader falls back to what it sent.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class SessionCreatedReplyJsonTests
{
    /// <summary>A reply as the API writes it: snake_case names, the time in UTC.</summary>
    private static readonly SessionCreatedReply Reply = new("sess_1", new DateTimeOffset(2026, 11, 11, 9, 0, 0,
            TimeSpan.Zero), ["alpha", "beta"], 1, "alpha-cook", "alpha", null, null, "sub-session:alpha", "run-1", null,
        "turn-1");

    /// <summary>The names on the wire are the snake_case ones every client reads.</summary>
    [Test]
    public void TheApisReply_WritesSnakeCaseNames()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Reply, SessionJson.SnakeCase));

        Assert.That(json.RootElement.EnumerateObject().Select(p => p.Name),
            Is.EqualTo(new[]
            {
                "session_id", "created_at", "toolsets", "depth", "role", "identity", "llm", "tool", "task", "run",
                "service", "parent"
            }));
    }

    /// <summary>What the API writes reads back as the same reply.</summary>
    [Test]
    public void TheApisReply_ReadsBackWhole()
    {
        var read = JsonSerializer.Deserialize<SessionCreatedReply>(
            JsonSerializer.Serialize(Reply, SessionJson.SnakeCase), SessionJson.SnakeCase)!;

        Assert.That((read with { Toolsets = Reply.Toolsets }, read.Toolsets), Is.EqualTo((Reply, Reply.Toolsets)));
    }

    /// <summary>A reply with only an id reads, its role and toolsets empty, so the reader keeps what it sent.</summary>
    [Test]
    public void AReplyWithOnlyAnId_Reads_TheRestEmpty()
    {
        var read = JsonSerializer.Deserialize<SessionCreatedReply>("""{"session_id":"sess_child"}""",
            SessionJson.SnakeCase)!;

        Assert.That((read.SessionId, read.Role, read.Toolsets), Is.EqualTo(("sess_child", (string?)null,
            (IReadOnlyList<string>?)null)));
    }
}

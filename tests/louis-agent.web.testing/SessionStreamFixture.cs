using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Testing;

/// <summary>
/// One recorded Puls turn (benchmarks/fixtures/session-stream.sse, F31 step 7a: a pancake recipe for two, priced near
/// Stellenbosch, handed to the cook), replayed through <see cref="TurnStream"/> the way the chat page replays the live
/// stream. Every component snapshot renders the turn it gives, so the goldens show a real answer, not a made-up one.
/// </summary>
public static class SessionStreamFixture
{
    /// <summary>The question the recording answered.</summary>
    public const string Question = "Gee my 'n eenvoudige resep vir pannekoek vir twee mense, en wat dit by 'n winkel naby Stellenbosch kos.";

    public static string Folder => Path.Combine(TestPaths.RepoRoot, "benchmarks", "fixtures");

    public static string StreamFile => Path.Combine(Folder, "session-stream.sse");

    /// <summary>The stream's events, in order, as the API client hands them to the page.</summary>
    public static async Task<List<SessionEvent>> EventsAsync()
    {
        var events = new List<SessionEvent>();
        await using var stream = File.OpenRead(StreamFile);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, CancellationToken.None))
            events.Add(streamEvent);
        return events;
    }

    /// <summary>
    /// The turn the stream folds into, with every thinking block's seconds fixed: the page measures them with the clock,
    /// which a snapshot can't depend on.
    /// </summary>
    public static async Task<Turn> ReplayAsync()
    {
        var turn = new Turn { UserText = Question };
        foreach (var streamEvent in await EventsAsync()) TurnStream.Apply(turn, streamEvent);
        FixThinking(turn);
        return turn;
    }

    public static void FixThinking(Turn turn)
    {
        foreach (var block in turn.Blocks)
        {
            if (block.Kind == BlockKind.Thinking) block.ThinkingSeconds = 2;
            if (block.SubTurn is { } sub) FixThinking(sub);
        }
    }

    /// <summary>What both readers of the stream must read from it (session-stream.expected.json).</summary>
    public static JsonElement Expected() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "session-stream.expected.json"))).RootElement;
}

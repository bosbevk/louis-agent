using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Testing;
using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Testing;

/// <summary>
/// One recorded Puls turn (session-stream.sse in the session-stream tests' fixtures: a pancake recipe for two, priced
/// near Stellenbosch, handed to the cook), replayed through <see cref="TurnStream"/> the way the chat page replays the
/// live stream. Every component snapshot renders the turn it gives, so the goldens show a real answer.
/// </summary>
public static class SessionStreamFixture
{
    /// <summary>
    /// The role the recording's cook ran as (recorded data stays as recorded): the goldens and the tests that replay
    /// the turn read it, so they name it here, once.
    /// </summary>
    public const string CookRole = "puls-cook";

    /// <summary>The question the recording answered.</summary>
    public const string Question =
        "Gee my 'n eenvoudige resep vir pannekoek vir twee mense, en wat dit by 'n winkel naby Stellenbosch kos.";

    /// <summary>Where the recording lives: the session-stream tests' fixtures, read from the source tree.</summary>
    public static string Folder =>
        Path.Combine(TestPaths.RepoRoot, "src/tests/engine/louis-agent.session-stream.tests", "Fixtures");

    public static string StreamFile => Path.Combine(Folder, "session-stream.sse");

    /// <summary>
    /// A second recording (F127.3.1, 2026-11-11): the site's guide asking Seemeeu, one of the showroom's businesses,
    /// with <c>AskAgent</c> over the site's own address, Seemeeu's events relayed under the call.
    /// </summary>
    public static string AskStreamFile => Path.Combine(Folder, "ask-agent.sse");

    /// <summary>What the visitor asked the guide in the second recording.</summary>
    public const string AskQuestion =
        "I want to stay at the Seemeeu guesthouse with my dog. Can you ask them whether dogs are allowed, and in " +
        "which room?";

    /// <summary>The Puls stream's events, in order, as the API client hands them to the page.</summary>
    public static Task<List<SessionEvent>> EventsAsync() => EventsAsync(StreamFile);

    /// <summary>
    /// The Puls turn the stream folds into, with every thinking block's seconds fixed: the page measures them with the
    /// clock, which a snapshot can't depend on.
    /// </summary>
    public static Task<Turn> ReplayAsync() => ReplayAsync(StreamFile, Question);

    /// <summary>The guide's turn that asked Seemeeu, folded the same way.</summary>
    public static Task<Turn> ReplayAskAsync() => ReplayAsync(AskStreamFile, AskQuestion);

    /// <summary>A recording's events, in order.</summary>
    private static async Task<List<SessionEvent>> EventsAsync(string file)
    {
        var events = new List<SessionEvent>();
        await using var stream = File.OpenRead(file);
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, CancellationToken.None))
            events.Add(streamEvent);
        return events;
    }

    /// <summary>A recording folded into the turn the page would show, its thinking seconds fixed.</summary>
    private static async Task<Turn> ReplayAsync(string file, string question)
    {
        var turn = new Turn { UserText = question };
        foreach (var streamEvent in await EventsAsync(file)) TurnStream.Apply(turn, streamEvent);
        FixThinking(turn);
        return turn;
    }

    /// <summary>Sets every thinking block's seconds to the same number, in sub-turns too.</summary>
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

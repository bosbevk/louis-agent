using System.Net.ServerSentEvents;
using System.Text.Json;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chat;

namespace LouisAgent.Puls.Tests.Fixtures;

/// <summary>
/// The recorded Puls turn (benchmarks/fixtures/session-stream.sse: a meal question handed to the cook), replayed through
/// <see cref="TurnStream"/> as the chat page replays the live stream. The same helper as the shared library's tests
/// keep: no test project references another, so the fifteen lines are here twice.
/// </summary>
internal static class SessionStreamFixture
{
    // The question the recording answered: a pancake recipe for two, priced at a shop near Stellenbosch.
    public const string Question = "Gee my 'n eenvoudige resep vir pannekoek vir twee mense, en wat dit by 'n winkel naby Stellenbosch kos.";

    public static string Folder => Path.Combine(TestPaths.RepoRoot, "benchmarks", "fixtures");

    public static string StreamFile => Path.Combine(Folder, "session-stream.sse");

    public static async Task<Turn> ReplayAsync()
    {
        var turn = new Turn { UserText = Question };
        await using var stream = File.OpenRead(StreamFile);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync())
            TurnStream.Apply(turn, new AgentStreamEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement));
        FixThinking(turn);
        return turn;
    }

    /// <summary>The page measures a thinking block with the clock, which a snapshot can't depend on.</summary>
    public static void FixThinking(Turn turn)
    {
        foreach (var block in turn.Blocks)
        {
            if (block.Kind == BlockKind.Thinking) block.ThinkingSeconds = 2;
            if (block.SubTurn is { } sub) FixThinking(sub);
        }
    }
}

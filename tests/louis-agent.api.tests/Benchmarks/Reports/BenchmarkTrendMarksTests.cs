using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Testing;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// A trend point says why a run might have moved: the notes people wrote on it, and which agents' prompts changed
/// since the run before it on the same line. Prompts are compared agent by agent, never as one list.
/// </summary>
public class BenchmarkTrendMarksTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
    private const string Cook = "sub-session:puls-cook";
    private InMemoryBenchmarkStore _store = null!;

    /// <summary>A store per test, so one test's runs aren't another's.</summary>
    [SetUp]
    public void SetUp() => _store = new InMemoryBenchmarkStore();

    /// <summary>
    /// A finished run of the cook check with one conversation in each of two modes, and the prompts its agents used.
    /// </summary>
    private async Task Run(string id, int hour, IReadOnlyList<AgentPrompt>? prompts)
    {
        await _store.SaveRunAsync(new Run(id, "cook", 1, BenchmarkRunStatus.Done, T0.AddHours(hour),
            Prompts: prompts));

        var scores = JsonDocument.Parse("""{"routed_right":true}""").RootElement.Clone();
        foreach (string mode in new[] { SampleMode.Alpha, SampleMode.Beta })
            await _store.SaveConversationAsync(id,
                new Conversation(new ConversationKey(mode, 1, SampleScenario.Alpha), 1, null, scores, 0, 1));
    }

    /// <summary>One agent's prompts on the cook's line.</summary>
    private static AgentPrompt P(string agent, params string[] hashes) => new(SampleMode.Beta, agent, hashes);

    /// <summary>The cook line's points, oldest first.</summary>
    private async Task<IReadOnlyList<BenchmarkTrends.Point>> CookLineAsync() =>
        (await BenchmarkTrends.ComputeAsync(_store, "cook")).Modes.Single(m => m.Name == SampleMode.Beta).Points;

    /// <summary>Two runs on the same prompts: compared, and nothing changed.</summary>
    [Test]
    public async Task TheSamePrompts_HaveNoMarker()
    {
        await Run("a", 1, [P("main", "m1"), P(Cook, "c1")]);
        await Run("b", 2, [P("main", "m1"), P(Cook, "c1")]);

        var points = await CookLineAsync();

        Assert.That(points[1].PromptChanged, Is.Empty);
    }

    /// <summary>The cook's prompt changed: the marker names the cook, and only the cook.</summary>
    [Test]
    public async Task OneAgentChanged_IsAMarkerNamingIt()
    {
        await Run("a", 1, [P("main", "m1"), P(Cook, "c1")]);
        await Run("b", 2, [P("main", "m1"), P(Cook, "c2")]);

        var points = await CookLineAsync();

        Assert.That(points[1].PromptChanged, Is.EqualTo(new[] { Cook }));
    }

    /// <summary>A run where the cook wasn't called has fewer prompts, and that isn't a change.</summary>
    [Test]
    public async Task AnAgentMissingFromOneRun_IsNoMarker()
    {
        await Run("a", 1, [P("main", "m1"), P(Cook, "c1")]);
        await Run("b", 2, [P("main", "m1")]);
        await Run("c", 3, [P("main", "m1"), P(Cook, "c1")]);

        var points = await CookLineAsync();

        Assert.That(points.Select(p => p.PromptChanged), Is.EqualTo(new[] { null, Array.Empty<string>(), [] }));
    }

    /// <summary>
    /// The first point has nothing before it, and a run stored before prompts were kept can't be compared.
    /// </summary>
    [Test]
    public async Task TheFirstPoint_AndARunWithoutAgentPrompts_HaveNoMarker()
    {
        await Run("a", 1, [P("main", "m1")]);
        await Run("old", 2, null);
        await Run("c", 3, [P("main", "m2")]);

        var points = await CookLineAsync();

        Assert.That(points.Select(p => p.PromptChanged), Is.All.Null, "unknown, never a false marker");
    }

    /// <summary>Another mode's prompts never mark this line.</summary>
    [Test]
    public async Task AChangeInAnotherMode_IsNoMarkerOnThisLine()
    {
        await Run("a", 1, [P("main", "m1"), new AgentPrompt(SampleMode.Alpha, "main", ["x1"])]);
        await Run("b", 2, [P("main", "m1"), new AgentPrompt(SampleMode.Alpha, "main", ["x2"])]);

        var trend = await BenchmarkTrends.ComputeAsync(_store, "cook");

        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Beta).Points[1].PromptChanged, Is.Empty);
        Assert.That(trend.Modes.Single(m => m.Name == SampleMode.Alpha).Points[1].PromptChanged,
            Is.EqualTo(new[] { "main" }));
    }

    /// <summary>
    /// A point carries the notes on its run and on its own mode's conversations, in the order they were written; a
    /// note on another mode's conversation stays on that mode's line.
    /// </summary>
    [Test]
    public async Task APoint_CarriesItsRunsNotes_AndItsModesConversationNotes()
    {
        await Run("a", 1, [P("main", "m1")]);
        await Run("b", 2, [P("main", "m1")]);
        await _store.AddNoteAsync("b", "judge changed", T0.AddHours(3));
        await _store.AddNoteAsync("b", "the cook guessed a price", T0.AddHours(4),
            new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha));
        await _store.AddNoteAsync("b", "routing was slow", T0.AddHours(5),
            new ConversationKey(SampleMode.Alpha, 1, SampleScenario.Alpha));

        var points = await CookLineAsync();

        Assert.That(points[0].Notes, Is.Empty);
        Assert.That(points[1].Notes, Is.EqualTo(new[] { "judge changed", "the cook guessed a price" }));
    }
}

using System.Text.Json;
using LouisAgent.Api.Config;
using LouisAgent.Api.Testing;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Runs.Store;
using LouisAgent.Evals.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using NUnit.Framework;

namespace LouisAgent.Api.Evals.Testing;

/// <summary>
/// Plays an eval back from the recordings kept in git, on a product's own API in memory, and holds that every fact of
/// every turn is met. Nothing calls a model and no key is needed, so a site's tests run it on every change: a change
/// to the agent asks the recording something it doesn't hold, and the test fails saying a live run is needed.
/// </summary>
/// <remarks>
/// Fowler's self-initializing fake, read back: the recording is made by a live run, and a miss is how it is found to
/// be stale. Only the facts are told, by code; the criteria want the judge, whose answers this replay leaves alone.
/// The product's API is built on its own catalogue and the settings its container runs with, so the agent asked is
/// the one its visitors get, and the run asks it over the sessions API with the key, as the eval routes do.
/// </remarks>
/// <seealso href="https://martinfowler.com/bliki/SelfInitializingFake.html">Fowler: Self Initializing Fake</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class EvalReplay
{
    private const string Key = "eval-replay-key";

    /// <summary>
    /// The eval replayed from its recordings on the product's catalogue, services and settings; any fact not met, or a
    /// recording that is missing or out of date, fails the test with a plain reason. The catalogue is made once the
    /// settings are set, since a pack reads its own when it is made.
    /// </summary>
    public static async Task AssertFactsMetAsync(Func<AgentCatalog> catalog, HostServices services,
        IReadOnlyDictionary<string, string> settings, string eval, string recordings)
    {
        string kept = Copied(recordings);
        // A pack reads its own settings from the process, so they are set for the replay and put back after.
        var before = settings.Keys.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        foreach (var (name, value) in settings) Environment.SetEnvironmentVariable(name, value);
        try
        {
            var recorded = RecordedTrials(kept);
            if (recorded.Count == 0)
                Assert.Fail($"Nothing is recorded in {recordings}: a live run of '{eval}' is needed.");

            var run = await ReplayAsync(catalog(), services, settings, eval, kept, recorded);
            if (run.Status != RunStatus.Finished)
                Assert.Fail($"The replay of '{eval}' could not finish: {run.StoppedBecause}");

            var unmet = Unmet(run);
            if (unmet.Count > 0)
                Assert.Fail($"Facts not met in the replay of '{eval}':\n  {string.Join("\n  ", unmet)}");
        }
        finally
        {
            foreach (var (name, value) in before) Environment.SetEnvironmentVariable(name, value);
            Directory.Delete(kept, recursive: true);
        }
    }

    /// <summary>The eval's conversations replayed, facts only, on the product's API in memory.</summary>
    private static async Task<EvalRun> ReplayAsync(AgentCatalog catalog, HostServices services,
        IReadOnlyDictionary<string, string> settings, string eval, string kept, IReadOnlyList<string> recorded)
    {
        string? Setting(string name) => settings.GetValueOrDefault(name);
        var agent = AgentOptions.FromEnvironment(Setting);
        agent.ReplayDirectory = kept;
        var host = AgentHost.Build(LlmOptions.FromEnvironment(Setting), agent, services.ClientFactory(), catalog,
            services);

        var file = EvalCatalog.Read(catalog).Find(eval);
        Assert.That(file, Is.Not.Null, $"The catalogue holds no eval named '{eval}'.");
        var factsOnly = FactsOnly(file!);

        await using var app = ApiTestHost.Build(apiKey: Key, openDoor: OpenDoorOptions.FromEnvironment(Setting),
            host: host);
        await app.StartAsync();
        var http = app.GetTestClient();
        http.DefaultRequestHeaders.Add("x-api-key", Key);

        // The run the recordings came from, and as many repeats as they hold.
        string replayOf = recorded[0][..recorded[0].IndexOf('/')];
        var started = new EvalRun(new RunId(DateTimeOffset.UtcNow, eval).Text, eval, file!.Agent.ToString(),
            RunKind.Replay, recorded.Count / file.Conversations.Count, DateTimeOffset.UtcNow, [])
        {
            ReplayOf = replayOf
        };
        var player = new ConversationPlayer(new SessionClient(http), new PageClient(http, new ReplayStore(kept)),
            new Judge(new NoJudge()));
        return await new EvalRunner(player, TimeProvider.System).RunAsync(started, factsOnly, _ => Task.CompletedTask);
    }

    /// <summary>The eval with its criteria left out, so only code tells the turns.</summary>
    private static EvalFile FactsOnly(EvalFile file) => file with
    {
        Conversations = file.Conversations.Select(conversation => conversation with
        {
            Turns = conversation.Turns.Select(turn => turn with { Criteria = [] }).ToList()
        }).ToList()
    };

    /// <summary>Every fact not met, with the conversation, the repeat and what code saw.</summary>
    private static IReadOnlyList<string> Unmet(EvalRun run) =>
        run.Conversations.SelectMany(conversation => conversation.Repeats.SelectMany(repeat => repeat.Turns
                .SelectMany(turn => turn.Facts).Where(mark => !mark.Met)
                .Select(mark => $"{conversation.Id}.{repeat.Number}: {mark.Text} ({mark.Reason})")))
            .ToList();

    /// <summary>The trials a recordings folder holds, by name, in order.</summary>
    private static IReadOnlyList<string> RecordedTrials(string folder)
    {
        string trials = Path.Combine(folder, "trials");
        if (!Directory.Exists(trials)) return [];

        return Directory.EnumerateFiles(trials, "*.jsonl").Select(File.ReadLines).Select(lines => lines.First())
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("trial").GetString()!)
            .Distinct().Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>A copy of the recordings, so a replay never writes into the folder git keeps.</summary>
    private static string Copied(string recordings)
    {
        string copy = Directory.CreateTempSubdirectory("eval-replay-").FullName;
        if (!Directory.Exists(recordings)) return copy;

        foreach (string file in Directory.EnumerateFiles(recordings, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(copy, Path.GetRelativePath(recordings, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        return copy;
    }

    /// <summary>A judge's model for a replay that marks no criterion, so it is never called.</summary>
    private sealed class NoJudge : IChatClient
    {
        /// <summary>Never called: the replay tells the facts only.</summary>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        /// <summary>Never called, for the same reason.</summary>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <summary>Has no services to offer.</summary>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <summary>Nothing to clean up.</summary>
        public void Dispose()
        {
        }
    }
}

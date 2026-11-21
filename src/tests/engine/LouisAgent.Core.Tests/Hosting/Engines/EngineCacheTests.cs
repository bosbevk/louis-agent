using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Engines;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;

namespace LouisAgent.Core.Tests.Hosting.Engines;

// The factory's cache (F5), moved out of AgentEngineFactory: one engine per key, built once outside the lock, and past the capacity the
// least recently used goes. Written first: EngineCache didn't exist on the red run.
public class EngineCacheTests
{
    private static ProfileKey Key(string name) =>
        ProfileKey.Of(
            [new ToolsetDefinition(name, "a toolset", GuideFile: null, MissingSettings: _ => [], CreateTools: _ => [])],
            null, RoleLevel.Agent, null, null);

    private static AgentEngine Engine() => new(new CompositeSkillProvider(), new FakeChatClient(),
        new AgentOptions { WorkspaceRoot = Path.GetTempPath() }, toolsets: []);

    [Test]
    public void ABuild_RunsOncePerKey_EvenForTwentyParallelCallsWithTheSameKey()
    {
        var cache = new EngineCache(16);
        int builds = 0;

        var engines = Enumerable.Range(0, 20).AsParallel().WithDegreeOfParallelism(20)
            .Select(_ => cache.GetOrAdd(Key(TestToolset.Files.Wire()), () =>
            {
                Interlocked.Increment(ref builds);
                Thread.Sleep(20);
                return Engine();
            })).ToList();

        Assert.That(builds, Is.EqualTo(1));
        Assert.That(engines.Distinct().Count(), Is.EqualTo(1), "every caller got the one engine");
    }

    [Test]
    public void PastTheCapacity_TheLeastRecentlyUsedGoes_NotTheOldest()
    {
        var cache = new EngineCache(2);
        var files = cache.GetOrAdd(Key(TestToolset.Files.Wire()), Engine);
        cache.GetOrAdd(Key(TestToolset.Git.Wire()), Engine);
        cache.GetOrAdd(Key(TestToolset.Files.Wire()), Engine); // files is now the more recent

        cache.GetOrAdd(Key(TestToolset.DotNet.Wire()), Engine);

        Assert.That(cache.Keys, Is.EquivalentTo(new[] { TestToolset.Files.Wire(), TestToolset.DotNet.Wire() }),
            "git, the least recently used, went");
        Assert.That(
            cache.GetOrAdd(Key(TestToolset.Files.Wire()),
                () => throw new InvalidOperationException("kept, so not built again")), Is.SameAs(files));
    }

    [Test]
    public void TheKeyBeingAdded_IsNeverTheOneDropped()
    {
        var cache = new EngineCache(1);
        cache.GetOrAdd(Key(TestToolset.Files.Wire()), Engine);

        var git = cache.GetOrAdd(Key(TestToolset.Git.Wire()), Engine);

        Assert.That(cache.Keys, Is.EqualTo(new[] { TestToolset.Git.Wire() }));
        Assert.That(cache.GetOrAdd(Key(TestToolset.Git.Wire()), () => throw new InvalidOperationException("kept")),
            Is.SameAs(git));
    }

    [Test]
    public void Keys_ListTheKeysText()
    {
        var cache = new EngineCache(16);
        var web = ProfileKey.Of(
        [
            new ToolsetDefinition(TestToolset.Web.Wire(), "a toolset", GuideFile: null, MissingSettings: _ => [],
                CreateTools: _ => [])
        ], "cook", RoleLevel.Specialist, "ollama-puls", null);

        cache.GetOrAdd(web, Engine);

        Assert.That(cache.Keys, Is.EqualTo(new[] { "web|cook|specialist|llm:ollama-puls" }));
        Assert.That(new EngineCache(16).Keys, Is.Empty);
    }
}

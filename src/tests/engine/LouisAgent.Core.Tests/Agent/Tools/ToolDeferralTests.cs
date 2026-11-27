using LouisAgent.Core.Agent.Tools;
using LouisAgent.Core.Config.ToolSearch;

namespace LouisAgent.Core.Tests.Agent.Tools;

/// <summary>
/// Which of a chat's tools wait to be found (F97.3): every tool but the ones its toolsets keep loaded and the engine's
/// own, and only when the provider can search and TOOL_SEARCH says so: on always, off never, auto past ten.
/// </summary>
public class ToolDeferralTests
{
    /// <summary>The engine's own tools, which always stay loaded.</summary>
    private static readonly string[] EngineOwn =
        ["FindTool", "GetToday", "UseSkill", "CreateSkill", "ReloadSkills", "ReadSkill"];

    /// <summary>A chat's tools: the engine's own, one kept loaded, and this many more.</summary>
    private static IEnumerable<string> Registered(int others) =>
        [.. EngineOwn, "GetStatus", .. Enumerable.Range(1, others).Select(i => $"Tool{i}")];

    /// <summary>A deferral that keeps GetStatus loaded.</summary>
    private static ToolDeferral Deferral(ToolSearchMode mode) => new(mode, new HashSet<string> { "GetStatus" });

    /// <summary>
    /// On, every tool but the kept and the engine's own waits; the engine's own and the kept never do.
    /// </summary>
    [Test]
    public void On_DefersEveryToolButTheKeptAndTheEnginesOwn()
    {
        var deferral = Deferral(ToolSearchMode.On);

        deferral.Decide(Registered(3), searchAvailable: true);

        Assert.Multiple(() =>
        {
            Assert.That(deferral.On, Is.True);
            Assert.That(deferral.Deferred, Is.EquivalentTo(new[] { "Tool1", "Tool2", "Tool3" }));
        });
    }

    /// <summary>
    /// Auto turns it on only past ten tools that could wait; off and a provider with no search never do.
    /// </summary>
    [TestCase(ToolSearchMode.Auto, 10, true, false)]
    [TestCase(ToolSearchMode.Auto, 11, true, true)]
    [TestCase(ToolSearchMode.Off, 50, true, false)]
    [TestCase(ToolSearchMode.On, 50, false, false)]
    public void WhetherItIsOn(ToolSearchMode mode, int others, bool available, bool on)
    {
        var deferral = Deferral(mode);

        deferral.Decide(Registered(others), available);

        Assert.Multiple(() =>
        {
            Assert.That(deferral.On, Is.EqualTo(on));
            Assert.That(deferral.Deferred, on ? Has.Count.EqualTo(others) : Is.Empty);
        });
    }
}

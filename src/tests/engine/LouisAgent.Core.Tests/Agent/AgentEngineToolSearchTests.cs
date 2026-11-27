using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Config.ToolSearch;
using LouisAgent.Core.Llm.ToolSearch;
using LouisAgent.Core.Prompting.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// The engine and its provider's tool search (F97.3): when search is on, the engine adds FindTool and hands the
/// provider every tool but the kept and its own to wait; on a provider with no search nothing changes.
/// </summary>
public class AgentEngineToolSearchTests
{
    /// <summary>A provider that can search, keeping what it was handed so the test can see it.</summary>
    private sealed class SearchingProvider : IToolSearch
    {
        /// <summary>The engine's answer to which tools wait, read when the test asks.</summary>
        public Func<IReadOnlySet<string>> Deferred { get; private set; } = () => new HashSet<string>();

        /// <inheritdoc />
        public bool Available => true;

        /// <inheritdoc />
        public IChatClient Around(IChatClient inner, Func<IReadOnlySet<string>> deferred)
        {
            Deferred = deferred;
            return inner;
        }
    }

    /// <summary>The git and file toolsets' objects, many tools between them.</summary>
    private static object[] Toolsets() => [new FakeGitTools(), new FakeFileTools(Path.GetTempPath())];

    /// <summary>
    /// An engine on those toolsets, keeping GetStatus loaded, with the search mode and provider given.
    /// </summary>
    private static AgentEngine Build(ToolSearchMode mode, IToolSearch search) =>
        AgentEngine.CreateBuilder(new CompositeSkillProvider(), new FakeChatClient(),
                new AgentOptions
                {
                    WorkspaceRoot = Path.GetTempPath(),
                    ToolSearch = mode
                })
            .WithToolsets(Toolsets())
            .WithToolSearch(search, new HashSet<string> { "GetStatus" })
            .Build();

    /// <summary>On, FindTool is there, and every tool but the kept and the engine's own waits.</summary>
    [Test]
    public void SearchOn_AddsFindTool_AndDefersTheRest()
    {
        var search = new SearchingProvider();

        var engine = Build(ToolSearchMode.On, search);

        var names = engine.Tools.Select(t => t.Name).ToList();
        var deferred = search.Deferred();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("FindTool"));
            Assert.That(deferred,
                Does.Not.Contain("GetStatus").And.Not.Contain("GetToday").And.Not.Contain("FindTool"));
            Assert.That(deferred, Is.EquivalentTo(names.Except(["GetStatus", "GetToday", "FindTool"])));
        });
    }

    /// <summary>Off, or a provider with no search, leaves the tools as they were, with no FindTool.</summary>
    [Test]
    public void SearchOff_OrNoSearch_LeavesTheToolsAlone()
    {
        var off = Build(ToolSearchMode.Off, new SearchingProvider());
        var none = Build(ToolSearchMode.On, NoToolSearch.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(off.Tools.Select(t => t.Name), Does.Not.Contain("FindTool"));
            Assert.That(none.Tools.Select(t => t.Name), Does.Not.Contain("FindTool"));
        });
    }
}

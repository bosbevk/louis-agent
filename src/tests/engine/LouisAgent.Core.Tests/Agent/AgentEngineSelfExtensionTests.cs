using System.Reflection;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Prompting.Providers;
using LouisAgent.Testing;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// F5 step 3: the self-extension tools are registered only when asked for on a toolsets engine; and, since F97.1, the
/// door to the skills an agent keeps, <c>UseSkill</c>, comes with them, so no other chat's tools grow.
/// </summary>
public sealed class AgentEngineSelfExtensionTests
{
    private static readonly string[] SelfExtensionTools = ["UseSkill", "CreateSkill", "ReloadSkills"];

    /// <summary>The tools every session has, whatever its toolsets: since F97.1 only the clock.</summary>
    private static readonly string[] EverySessionsTools = ["GetToday"];

    /// <summary>The files toolset's objects.</summary>
    private static object[] FilesTools() => [new FakeFileTools(Path.GetTempPath())];

    /// <summary>No toolsets and no self-extension is only the tools every session has.</summary>
    [Test]
    public void EmptyToolsetsGivesOnlyTheClock()
    {
        var engine = Build(toolsets: [], selfExtension: false);

        Assert.That(Names(engine), Is.EquivalentTo(EverySessionsTools), "the tools every session has");
    }

    /// <summary>Self-extension alone gives its tools, the door among them, and every session's.</summary>
    [Test]
    public void SelfExtensionWithNoToolsetsGivesOnlyItsTools()
    {
        var engine = Build(toolsets: [], selfExtension: true);

        Assert.That(Names(engine), Is.EquivalentTo(SelfExtensionTools.Concat(EverySessionsTools)));
    }

    /// <summary>A toolset with self-extension gives both.</summary>
    [Test]
    public void FilesPlusSkillsGivesWorkspaceToolsAndSelfExtension()
    {
        var engine = Build(toolsets: FilesTools(), selfExtension: true);

        var expected = FilesTools().SelectMany(t => PublicMethodNames(t.GetType()))
            .Concat(SelfExtensionTools)
            .Concat(EverySessionsTools);
        Assert.That(Names(engine), Is.EquivalentTo(expected));
    }

    /// <summary>A toolset without self-extension has none of its tools, the door included.</summary>
    [Test]
    public void FilesWithoutSkillsHasNoSelfExtension()
    {
        var engine = Build(toolsets: FilesTools(), selfExtension: false);

        Assert.That(Names(engine).Intersect(SelfExtensionTools), Is.Empty);
        Assert.That(Names(engine), Does.Not.Contain("ExecuteSkill"), "F97.1: no second door to skills");
    }

    /// <summary>The default path, no toolsets named, gives the self-extension tools and every session's.</summary>
    [Test]
    public void DefaultPathRegistersSelfExtensionOnly()
    {
        var engine = Build(toolsets: null, selfExtension: false);

        Assert.That(Names(engine), Is.EquivalentTo(SelfExtensionTools.Concat(EverySessionsTools)));
    }

    /// <summary>An engine with these toolsets, on a model that is never asked.</summary>
    private static AgentEngine Build(IEnumerable<object>? toolsets, bool selfExtension) =>
        new(new CompositeSkillProvider(), new FakeChatClient(),
            new AgentOptions { WorkspaceRoot = Path.GetTempPath() }, toolsets: toolsets, selfExtension: selfExtension);

    /// <summary>The engine's tools, by name.</summary>
    private static string[] Names(AgentEngine engine) => engine.Tools.Select(t => t.Name).ToArray();

    /// <summary>A tool class's public methods, which become its tools.</summary>
    private static string[] PublicMethodNames(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();
}

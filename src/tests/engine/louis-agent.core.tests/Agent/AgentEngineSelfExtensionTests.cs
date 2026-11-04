namespace LouisAgent.Core.Tests;

using System.Reflection;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Documents;
using LouisAgent.Core.Tools.Workspace;
using Microsoft.Extensions.AI;
using NUnit.Framework;

/// <summary>F5 step 3: the self-extension tools are registered only when asked for on a toolsets engine.</summary>
[TestFixture]
public sealed class AgentEngineSelfExtensionTests
{
    private static readonly string[] SelfExtensionTools = ["CreateSkillFile", "ReloadSkills", "CreateTool", "ListAgentTools"];

    /// <summary>The files toolset's objects (S, CL15: five classes), on a root the tests never write to.</summary>
    private static object[] FilesTools() =>
    [
        new WorkspaceFileTools(Path.GetTempPath()), new WorkspaceOrganiseTools(Path.GetTempPath()), new WorkspaceBrowseTools(Path.GetTempPath()),
        new WorkspaceSearchTools(Path.GetTempPath()), new DocumentTools(Path.GetTempPath()),
    ];

    [Test]
    public void EmptyToolsetsGivesOnlyExecuteSkill()
    {
        var engine = Build(toolsets: [], selfExtension: false);

        Assert.That(Names(engine), Is.EquivalentTo(new[] { "ExecuteSkill", "GetToday" }), "the tools every session has");
    }

    [Test]
    public void SelfExtensionWithNoToolsetsGivesOnlyItsTools()
    {
        var engine = Build(toolsets: [], selfExtension: true);

        Assert.That(Names(engine), Is.EquivalentTo(SelfExtensionTools.Append("ExecuteSkill").Append("GetToday")));
    }

    [Test]
    public void FilesPlusSkillsGivesWorkspaceToolsAndSelfExtension()
    {
        var engine = Build(toolsets: FilesTools(), selfExtension: true);

        var expected = FilesTools().SelectMany(t => PublicMethodNames(t.GetType()))
            .Concat(SelfExtensionTools)
            .Append("ExecuteSkill").Append("GetToday");
        Assert.That(Names(engine), Is.EquivalentTo(expected));
    }

    [Test]
    public void FilesWithoutSkillsHasNoSelfExtension()
    {
        var engine = Build(toolsets: FilesTools(), selfExtension: false);

        Assert.That(Names(engine).Intersect(SelfExtensionTools), Is.Empty);
        Assert.That(Names(engine), Does.Contain("ExecuteSkill"));
    }

    [Test]
    public void DefaultPathRegistersSelfExtensionOnly()
    {
        // toolsets: null is the default path: ExecuteSkill and the self-extension tools, no built-in toolsets.
        var engine = Build(toolsets: null, selfExtension: false);

        Assert.That(Names(engine), Is.EquivalentTo(SelfExtensionTools.Append("ExecuteSkill").Append("GetToday")));
    }

    private static AgentEngine Build(IEnumerable<object>? toolsets, bool selfExtension) =>
        new(new CompositeSkillProvider(), new NoModelClient(), new AgentOptions { WorkspaceRoot = Path.GetTempPath() },
            toolsets: toolsets, selfExtension: selfExtension);

    private static string[] Names(AgentEngine engine) => engine.Tools.Select(t => t.Name).ToArray();

    private static string[] PublicMethodNames(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

    /// <summary>Only the members the engine calls in these tests are meaningful; none of them reach a model.</summary>
    private sealed class NoModelClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}

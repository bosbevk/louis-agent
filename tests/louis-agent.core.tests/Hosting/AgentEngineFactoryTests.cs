using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Hosting;

// F5 step 5: one engine per toolset profile, cached by its normalised list, with only its tools and its guides.
public class AgentEngineFactoryTests
{
    private static AgentOptions Options(string? paymoKey = null) => new()
    {
        WorkspaceRoot = TestPaths.RepoRoot, SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills"),
        PaymoApiKey = paymoKey, RatesUrl = null,
    };

    private static AgentEngineFactory Factory(int capacity = AgentEngineFactory.DefaultCapacity, string? paymoKey = null) =>
        new(new FakeChatClient(), Options(paymoKey), capacity: capacity);

    private static string[] Names(AgentEngine engine) => engine.Tools.Select(t => t.Name).ToArray();

    [Test]
    public void TheSameList_InAnyOrderOrCase_GivesTheSameEngine()
    {
        var factory = Factory();

        var first = factory.GetOrCreate(["git", "files"]);
        var second = factory.GetOrCreate(["Files", " git ", "files"]);

        Assert.That(second, Is.SameAs(first));
        Assert.That(factory.Profiles, Is.EqualTo(new[] { "files,git" }));
    }

    [Test]
    public void DifferentLists_GiveDifferentTools()
    {
        var factory = Factory();

        var files = factory.GetOrCreate(["files"]);
        var git = factory.GetOrCreate(["git"]);

        Assert.That(Names(files), Does.Contain("ReadWorkspaceFile").And.Not.Contain("GetStatus"));
        Assert.That(Names(git), Does.Contain("GetStatus").And.Not.Contain("ReadWorkspaceFile"));
        Assert.That(Names(files), Does.Contain("ExecuteSkill").And.Not.Contain("CreateTool"), "no skills toolset, no self-extension");
    }

    [Test]
    public void TheSkillsToolset_AddsTheSelfExtensionTools()
    {
        Assert.That(Names(Factory().GetOrCreate(["files", "skills"])), Does.Contain("CreateTool").And.Contain("CreateSkillFile"));
    }

    [Test]
    public void AnEngine_HasOnlyItsToolsetsGuides_AndReloadingKeepsThem()
    {
        var engine = Factory().GetOrCreate(["files", "skills"]);

        Assert.That(engine.SkillDocumentation, Does.Contain("# Files Skills").And.Not.Contain("# Python Skills"));
        engine.ReloadSkills();
        Assert.That(engine.SkillDocumentation, Does.Contain("# Files Skills").And.Not.Contain("# Python Skills"));
    }

    [Test]
    public void AnUnknownOrUnavailableToolset_ThrowsBeforeAnythingIsBuilt()
    {
        var factory = Factory();

        Assert.That(() => factory.GetOrCreate(["files", "nope"]), Throws.TypeOf<ToolsetException>().With.Message.Contain("Valid names"));
        Assert.That(() => factory.GetOrCreate(["paymo"]), Throws.TypeOf<ToolsetException>().With.Message.Contain("PAYMO_API_KEY"));
        Assert.That(factory.Profiles, Is.Empty);
    }

    [Test]
    public void AFullCache_DropsTheLeastRecentlyUsed_NotTheNewList()
    {
        // PR #27 review: on a specialist the model chooses the lists; refusing the 17th would stop it until a restart.
        var factory = Factory(capacity: 2);
        var files = factory.GetOrCreate(["files"]);
        factory.GetOrCreate(["git"]);
        factory.GetOrCreate(["files"]);   // files is now the more recent

        var dotnet = factory.GetOrCreate(["dotnet"]);

        Assert.That(dotnet, Is.Not.Null);
        Assert.That(factory.Profiles, Is.EquivalentTo(new[] { "files", "dotnet" }), "git, the least recently used, went");
        Assert.That(factory.GetOrCreate(["files"]), Is.SameAs(files), "a kept profile is the same engine");
        Assert.That(factory.GetOrCreate(["git"]), Is.Not.Null, "a dropped one is built again");
    }

    [Test]
    public void Available_ListsOnlyToolsetsWhoseSettingsAreSet()
    {
        Assert.That(Factory().Available, Does.Contain("files").And.Not.Contain("paymo"));
        Assert.That(Factory(paymoKey: "key").Available, Does.Contain("paymo"));
    }

    [Test]
    public void NoToolsets_GivesOnlyExecuteSkill()
    {
        Assert.That(Names(Factory().GetOrCreate([])), Is.EqualTo(new[] { "ExecuteSkill" }));
    }
}
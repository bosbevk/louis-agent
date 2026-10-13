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

    /// <summary>A factory whose skills folder is a temp one holding personalities/factory-voice.md (F21); the guides and
    /// the router come from the embedded copies.</summary>
    private static AgentEngineFactory FactoryWithVoice(out string skills)
    {
        skills = Path.Combine(Path.GetTempPath(), "factory-voice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(skills, "personalities"));
        File.WriteAllText(Path.Combine(skills, "personalities", "factory-voice.md"), "# Factory Voice");
        var options = Options();
        options.SkillsDirectory = skills;
        return new AgentEngineFactory(new FakeChatClient(), options);
    }

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
    public void APersonality_IsItsOwnProfile_WithItsOwnPrompt()
    {
        // F21: the same toolsets with another personality, or without the router, is another engine.
        var factory = FactoryWithVoice(out string skills);
        try
        {
            var plain = factory.GetOrCreate(["files"]);
            var voiced = factory.GetOrCreate(["files"], "factory-voice");
            var noRouter = factory.GetOrCreate(["files"], router: false);

            Assert.That(voiced, Is.Not.SameAs(plain));
            Assert.That(factory.GetOrCreate(["files"], "factory-voice"), Is.SameAs(voiced));
            Assert.That(voiced.SkillDocumentation, Does.Contain("# Factory Voice").And.Not.Contain("Louis's Minion"));
            Assert.That(noRouter.SkillDocumentation, Does.Not.Contain("# Louis Agent - Default Instructions"));
            Assert.That(plain.SkillDocumentation, Does.Contain("# Louis Agent - Default Instructions"));
            Assert.That(factory.Profiles, Is.EquivalentTo(new[] { "files", "files|factory-voice", "files|no-router" }));
            Assert.That(factory.AvailablePersonalities, Does.Contain("factory-voice"));
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    [Test]
    public void AnUnknownPersonality_IsRefused_BeforeAnythingIsBuilt()
    {
        var factory = Factory();

        Assert.Throws<LouisAgent.Core.Skills.PersonalityException>(() => factory.GetOrCreate(["files"], "nobody-here"));
        Assert.That(factory.Profiles, Is.Empty);
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
    public void TheCap_RefusesANewProfile_ButNotAnExistingOne()
    {
        var factory = Factory(capacity: 2);
        factory.GetOrCreate(["files"]);
        factory.GetOrCreate(["git"]);

        Assert.That(() => factory.GetOrCreate(["dotnet"]), Throws.TypeOf<ToolsetException>().With.Message.Contain("2 toolset profiles"));
        Assert.That(factory.GetOrCreate(["files"]), Is.Not.Null);
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
        Assert.That(Names(Factory().GetOrCreate([])), Is.EquivalentTo(new[] { "ExecuteSkill", "GetToday" }));
    }
}
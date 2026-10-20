using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

// F21 step 1: a session names its role (its personality before CL13); the prompt starts with that one instead of personality.md, and a name
// never reaches outside its level's folder (CL8: Skills/agent/, Skills/specialists/). CL15 step 3: the mechanism is
// tested on the tests' own roles (Prompts/: testprod); Puls's content is tested with Puls, in its identity's tests.
public class RoleCatalogTests
{
    private const string Modes = "louis-agent/voice, louis-agent/voice-orchestrator, testprod/voice-af, testprod/voice-en";

    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _skills = Path.Combine(Path.GetTempPath(), "roles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_skills, RoleCatalog.AgentFolder));
        File.WriteAllText(Path.Combine(_skills, RoleCatalog.AgentFolder, "test-voice.md"), "# Test Voice\n\nKort en kragtig.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    private AgentOptions Options() => new() { SkillsDirectory = _skills };

    [Test]
    public void NoName_IsTheDefaultMode()
    {
        string docs = SkillLoader.Load(Options(), [], role: null).Documentation;

        Assert.That(docs, Does.Contain("Louis's Minion"), "the default voice, louis-agent/voice (personality.md before CL9)");
        Assert.That(docs, Does.Not.Contain("# Testprod - Stem"));
        Assert.That(docs, Does.Contain("# Louis Agent - Default Instructions"), "and its router");
    }

    [Test]
    public void AMode_IsOneFile_ItsVoiceThenItsRouting()
    {
        // CL9: a level-0 session loads one file; its voice frames everything after it, its routing follows.
        string docs = SkillLoader.Load(new AgentOptions(), [], "testprod/voice-af").Documentation;

        Assert.That(RoleCatalog.DefaultVoice, Is.EqualTo("louis-agent/voice"));
        Assert.That(docs.IndexOf("# Testprod - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)));
        Assert.That(SkillFileSource.ReadEmbedded("agent/testprod/voice.md"), Does.StartWith("# Testprod - Routing"), "the product's shared part");
    }

    [Test]
    public void ANamedRole_ReplacesTheDefault_AndComesFirst()
    {
        string docs = SkillLoader.Load(Options(), [], "testprod/voice-af").Documentation;

        Assert.That(docs, Does.Contain("# Testprod - Stem"));
        Assert.That(docs, Does.Not.Contain("Louis's Minion"));
        Assert.That(docs.IndexOf("# Testprod - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)), "who the agent is, before its routing");
    }

    [TestCase("../personality")]
    [TestCase("Testprod")]
    [TestCase("a/b/c")]
    [TestCase("voice.md")]
    [TestCase("-voice")]
    public void ANameThatIsntValid_IsRefused(string name)
    {
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], name));
        Assert.That(ex!.Message, Does.Contain("lowercase letters, digits and dashes"));
    }

    [Test]
    public void AnUnknownName_IsRefused_ListingTheAvailable()
    {
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], "nobody"));
        Assert.That(ex!.Message, Does.Contain("Unknown role 'nobody'").And.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void AVoiceOnDisk_IsNeitherOfferedNorLoaded()
    {
        // CL8 step 4: roles are built in; the skills folder holds only what the agent writes.
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Does.Not.Contain("test-voice"));
        Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], "test-voice"));
    }

    [Test]
    public void Core_EmbedsNoRoleOfAProduct_UntilAnIdentityBringsIt()
    {
        // CL15 step 3 (the cut): Puls's modes, specialists and minions live in its identity. Without it registered (these
        // tests register only their own roles) no level lists a Puls role; the identity's tests show the other side.
        foreach (var level in Enum.GetValues<RoleLevel>())
            Assert.That(RoleCatalog.Available(level), Has.None.StartsWith("puls"), $"level {level}");
        Assert.That(SkillFileSource.CoreFiles, Has.None.Contains("puls"), "core's own prompts name no product file");
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "puls/voice-af"));
    }

    [Test]
    public void TheCodingAgent_RoutesOnlyWhatItDoes()
    {
        // CL9 step 3: a product's routing holds its own rows, so neither pays for the other's in every request.
        string louis = RoleCatalog.ModeParts(SkillFileSource.ReadEmbedded("agent/louis-agent/voice.md")!).Routing;

        Assert.That(louis, Does.Not.Contain("puls-cook").And.Not.Contain("**Vir:**").And.Not.Contain("cook"), "no cook, no Afrikaans briefing");
    }

    [TestCase("af", "# Testprod - Stem")]
    [TestCase("en", "# Testprod - Voice")]
    public void ALanguage_LoadsItsVoice_ThenTheProductsShared(string code, string voice)
    {
        // CL9 step 6 (Louis: "a puls/voice.md for shared things"): the language file says how the product talks, voice.md routes.
        string docs = SkillLoader.Load(new AgentOptions(), [], $"testprod/voice-{code}").Documentation;

        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.GreaterThan(0));
        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)));
        Assert.That(RoleCatalog.LanguageOf($"testprod/voice-{code}"), Is.EqualTo(code));
    }

    [Test]
    public void AProductsSharedVoice_IsNotAModeOnItsOwn()
    {
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(Modes.Split(", ")));
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "testprod/voice"));
        Assert.That(RoleCatalog.LanguageOf("louis-agent/voice"), Is.Null, "the coding agent's mode is whole");
    }

    [TestCase("functions/code-review")]
    [TestCase("testprod")]
    [TestCase("default")]
    public void OnlyAMode_IsANameAtLevelZero(string name)
    {
        // CL9: a mode is agent/{product}/{mode}.md; a function skill, a product folder or an old name isn't one.
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], name));

        Assert.That(ex!.Message, Does.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void EachLevel_ListsOnlyItsOwnRoles()
    {
        // CL8: the folders follow the levels; F30 step 4 fills minions/ (level 2) with its first role.
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist), Is.EqualTo(new[] { "testprod-specialist" }));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Is.EqualTo(new[] { "testprod-minion-lookup" }));
    }

    [Test]
    public void ASpecialistsRole_IsRefusedAtLevelZero_NamingTheVoicesThereAre()
    {
        // CL8: a role opens only at its own level, so a chat can't take a specialist's role as its voice.
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "testprod-specialist", RoleLevel.Agent));

        Assert.That(ex!.Message, Does.Contain("Unknown role 'testprod-specialist'").And.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void TheAgentsVoice_IsRefusedAtLevelOne_NamingTheRolesThereAre()
    {
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "testprod/voice-af", RoleLevel.Specialist));

        Assert.That(ex!.Message, Does.Contain("Unknown role 'testprod/voice-af'").And.Contain("testprod-specialist"));
    }

    [Test]
    public void OnlyLevelZero_HasTheRouter()
    {
        // The router picks toolsets and delegates; a specialist's toolsets were chosen for it (F21), and so are a minion's.
        Assert.That(SkillLoader.Load(new AgentOptions(), [], level: RoleLevel.Agent).Documentation,
            Does.Contain("# Louis Agent - Default Instructions"));
        Assert.That(SkillLoader.Load(new AgentOptions(), [], level: RoleLevel.Specialist).Documentation,
            Does.Not.Contain("# Louis Agent - Default Instructions"));
    }

    [Test]
    public void TheSpecialistsGuide_IsTheMechanism_NotAProductsProtocol()
    {
        // CL15 step 3: the guide every identity with specialists reads says how to start, continue and relay; Puls's
        // briefing labels, its roles table, its waiting roles and its Afrikaans lines moved to Puls's own routing.
        string guide = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;

        Assert.That(guide, Does.Contain("Every sub-session costs money"), "the one choosing is told the cost");
        Assert.That(guide, Does.Contain("## Following up").And.Contain("`ContinueSubSession(handle, message, attachments)`"));
        Assert.That(guide, Does.Contain("Continue, don't answer it yourself"));
        Assert.That(guide, Does.Contain("## Relaying a report").And.Contain("as the specialist wrote it").And.Contain("every table row"));
        Assert.That(guide, Does.Contain("The footer the tool adds under the report").And.Contain("never in your answer"));
        Assert.That(guide, Does.Contain("Pass `role`").And.Not.Contain("personality").IgnoreCase, "CL14: the model's word is role");
        Assert.That(guide, Does.Not.Contain("puls").IgnoreCase.And.Not.Contain("Vir hoeveel").And.Not.Contain("stemming").And.Not.Contain("Counting is the analyst's"));
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/specialists.md"), Does.Not.Contain("roles.json"), "CL10: a role is named, never derived");
        Assert.That(SkillFileSource.ReadEmbedded("specialists/roles.json"), Is.Null);
    }

    [Test]
    public void TheMinionsGuide_NamesNoProductsRole()
    {
        string guide = SkillFileSource.ReadEmbedded("toolsets/minions.md")!;

        Assert.That(guide, Does.Contain("`StartMinion(role, tool, task, stronger)`"));
        Assert.That(guide, Does.Not.Contain("puls").IgnoreCase.And.Not.Contain("Malmesbury"));
    }

    [TestCase("agent/louis-agent/voice.md", "# Louis's Minion - Voice")]
    public void AVoicesHeading_SaysVoice(string file, string heading) =>
        Assert.That(SkillFileSource.ReadEmbedded(file)!, Does.StartWith(heading));
}

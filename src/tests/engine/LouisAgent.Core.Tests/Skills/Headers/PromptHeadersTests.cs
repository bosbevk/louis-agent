using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Core.Tests.Skills.Headers;

/// <summary>
/// CL27: every built-in prompt opens with the Agent Skills spec's front matter, its name and description, and ours
/// (<c>level</c>, <c>toolsets</c>) under <c>metadata</c>. The reader gives the header and the body the model reads; a
/// file without one is read whole; a header that says something we don't read refuses the file, naming it.
/// </summary>
public class PromptHeadersTests
{
    private const string Path = "subagents/acme-cook.md", Name = "acme-cook";

    /// <summary>
    /// A role's file with its header: the web always, the shops where this instance has them. In LF, whatever line
    /// endings git gave this test file (FX33): a raw string takes the file's own, CRLF on a Windows checkout.
    /// </summary>
    private static string Cook => $"""
                                   ---
                                   name: {Name}
                                   description: Acme's local cook. A recipe for the people and town asked for.
                                   metadata:
                                     level: {RoleLevel.Subagent.Wire()}
                                     toolsets: {TestToolset.Web.Wire()}, shops?
                                   ---
                                   # Acme subagent: Local cook

                                   """.ReplaceLineEndings("\n");

    /// <summary>A header gives its fields, and the body is the text after it, as it was.</summary>
    [Test]
    public void AHeader_GivesItsFields_AndTheBody()
    {
        var file = PromptHeaders.Split(Path, Cook);

        Assert.That(file.Header, Is.EqualTo(new PromptHeader(Name,
            "Acme's local cook. A recipe for the people and town asked for.", RoleLevel.Subagent,
            [TestToolset.Web.Wire(), "shops?"])).Using<PromptHeader>(SameHeader));
        Assert.That(file.Body, Is.EqualTo("# Acme subagent: Local cook\n"));
    }

    /// <summary>Windows line endings are read the same, and the body keeps its own.</summary>
    [Test]
    public void WindowsLineEndings_AreReadTheSame()
    {
        var file = PromptHeaders.Split(Path, Cook.ReplaceLineEndings("\r\n"));

        Assert.That((file.Header?.Name, file.Body), Is.EqualTo((Name, "# Acme subagent: Local cook\r\n")));
    }

    /// <summary>
    /// The level below the agent is a subagent's (CL68.2, ADR 0133, Louis: "So it is actually a SubAgent"): a header
    /// says <c>subagent</c>, and the old word <c>specialist</c> is refused.
    /// </summary>
    [Test]
    public void ALevelOneHeader_SaysSubagent()
    {
        const string header = "---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  level: subagent\n---\n# Cook\n";

        Assert.That(PromptHeaders.Split(Path, header).Header!.Level, Is.EqualTo((RoleLevel)1));
    }

    /// <summary>
    /// The level below a subagent is a lookup agent's (CL68.3, ADR 0021, Louis: "Minions --> lookup-agents
    /// sub-agents"): a header says <c>lookup</c>, and the old word <c>minion</c> is refused.
    /// </summary>
    [Test]
    public void ALevelTwoHeader_SaysLookup()
    {
        const string header = "---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  level: lookup\n---\n# Cook\n";

        Assert.That(PromptHeaders.Split(Path, header).Header!.Level, Is.EqualTo((RoleLevel)2));
    }

    /// <summary>A guide or the principles name no level and no toolsets of their own.</summary>
    [Test]
    public void NoMetadata_IsNoLevel_AndNoToolsets()
    {
        var file = PromptHeaders.Split("shared/principles.md",
            "---\nname: principles\ndescription: What holds for every agent.\n---\n# Principles\n");

        Assert.That((file.Header?.Level, file.Header?.Toolsets.Count), Is.EqualTo(((RoleLevel?)null, 0)));
    }

    /// <summary>A file with no header (the agent's own function files) is read whole.</summary>
    [TestCase("# Acme subagent: Local cook\n\nYou are …")]
    [TestCase("")]
    [TestCase("--- not a header\ntext")]
    public void NoHeader_IsTheTextWhole(string text)
    {
        var file = PromptHeaders.Split("functions/note.md", text);

        Assert.That((file.Header, file.Body), Is.EqualTo(((PromptHeader?)null, text)));
    }

    /// <summary>What the header says wrong refuses the file, naming it and the fault.</summary>
    [TestCase("---\nname: acme-cook\ndescription: A cook.\ntoolset: files\n---\n# Cook\n", "toolset")]
    [TestCase("---\nname: acme-baker\ndescription: A cook.\n---\n# Cook\n", "acme-baker")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  level: chef\n---\n# Cook\n", "chef")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  level: specialist\n---\n# Cook\n",
        "specialist")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  level: minion\n---\n# Cook\n", "minion")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\nmetadata:\n  load: sometimes\n---\n# Cook\n", "sometimes")]
    [TestCase("---\nname: acme-cook\n---\n# Cook\n", "description")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\n# Cook\n", "---")]
    [TestCase("---\nname: [acme\n---\n# Cook\n", "acme-cook.md")]
    public void AFaultyHeader_RefusesTheFile(string text, string named)
    {
        var refused = Assert.Throws<PromptHeaderException>(() => PromptHeaders.Split(Path, text));

        Assert.That(refused!.Message, Does.Contain(Path).And.Contain(named));
    }

    /// <summary>
    /// A skill's header says how it loads (F129): always beside its voice, or on demand when the agent reads it; a
    /// prompt that isn't a skill says neither.
    /// </summary>
    [TestCase("always", SkillLoad.Always)]
    [TestCase("on-demand", SkillLoad.OnDemand)]
    public void ASkillsHeader_SaysHowItLoads(string load, SkillLoad expected)
    {
        var file = PromptHeaders.Split("agents/acme/skills/menu/SKILL.md",
            $"---\nname: menu\ndescription: The menu.\nmetadata:\n  level: agent\n  load: {load}\n---\n# Menu\n");

        Assert.That(file.Header!.Load, Is.EqualTo(expected));
    }

    /// <summary>
    /// A skill is a folder holding its <c>SKILL.md</c> (CL68.1, Louis: "each skill in it's folder, and SKILL.md"), and
    /// its name is its folder's, as Claude Code's skills are (ADR 0063); naming it after the file is refused.
    /// </summary>
    [Test]
    public void ASkillsName_IsItsFolders()
    {
        const string skill = "agents/acme/skills/menu/SKILL.md";
        const string body = "description: The menu.\nmetadata:\n  load: always\n---\n# Menu\n";

        Assert.That(PromptHeaders.Split(skill, "---\nname: menu\n" + body).Header!.Name, Is.EqualTo("menu"));
        Assert.That(Assert.Throws<PromptHeaderException>(() => PromptHeaders.Split(skill, "---\nname: SKILL\n" + body))!
            .Message, Does.Contain("'menu'"));
    }

    /// <summary>A prompt that names no load is no skill, so it has none.</summary>
    [Test]
    public void APromptWithoutLoad_HasNone() =>
        Assert.That(PromptHeaders.Split(Path, Cook).Header!.Load, Is.Null);

    /// <summary>
    /// F97.1: a skill the agent made names the script it runs, by its path from the skill's folder, as a plain string
    /// under <c>metadata</c>, the spec's place for a client's own properties; a prompt that names none has none.
    /// </summary>
    [Test]
    public void ASkillsHeader_NamesItsScript_AndAPromptWithoutOneHasNone()
    {
        var skill = PromptHeaders.Split("skills/word-count/SKILL.md",
            "---\nname: word-count\ndescription: Counts words.\nmetadata:\n  script: scripts/count.py\n---\n# Count\n");
        var prompt = PromptHeaders.Split(Path, Cook);

        Assert.That((skill.Header!.Script, prompt.Header!.Script), Is.EqualTo(("scripts/count.py", (string?)null)));
    }

    /// <summary>Two headers are the same when every field is, the toolsets in order.</summary>
    private static bool SameHeader(PromptHeader a, PromptHeader b) =>
        (a.Name, a.Description, a.Level, a.Load) == (b.Name, b.Description, b.Level, b.Load) &&
        a.Toolsets.SequenceEqual(b.Toolsets);
}

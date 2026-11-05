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
    private const string Path = "specialists/acme-cook.md", Name = "acme-cook";

    /// <summary>
    /// A role's file with its header: the web always, the shops where this instance has them. In LF, whatever line
    /// endings git gave this test file (FX33): a raw string takes the file's own, CRLF on a Windows checkout.
    /// </summary>
    private static string Cook => $"""
        ---
        name: {Name}
        description: Acme's local cook. A recipe for the people and town asked for.
        metadata:
          level: {RoleLevel.Specialist.Wire()}
          toolsets: {TestToolset.Web.Wire()}, shops?
        ---
        # Acme specialist: Local cook

        """.ReplaceLineEndings("\n");

    /// <summary>A header gives its fields, and the body is the text after it, as it was.</summary>
    [Test]
    public void AHeader_GivesItsFields_AndTheBody()
    {
        var file = PromptHeaders.Split(Path, Cook);

        Assert.That(file.Header, Is.EqualTo(new PromptHeader(Name,
            "Acme's local cook. A recipe for the people and town asked for.", RoleLevel.Specialist,
            [TestToolset.Web.Wire(), "shops?"])).Using<PromptHeader>(SameHeader));
        Assert.That(file.Body, Is.EqualTo("# Acme specialist: Local cook\n"));
    }

    /// <summary>Windows line endings are read the same, and the body keeps its own.</summary>
    [Test]
    public void WindowsLineEndings_AreReadTheSame()
    {
        var file = PromptHeaders.Split(Path, Cook.ReplaceLineEndings("\r\n"));

        Assert.That((file.Header?.Name, file.Body), Is.EqualTo((Name, "# Acme specialist: Local cook\r\n")));
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
    [TestCase("# Acme specialist: Local cook\n\nYou are …")]
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
    [TestCase("---\nname: acme-cook\n---\n# Cook\n", "description")]
    [TestCase("---\nname: acme-cook\ndescription: A cook.\n# Cook\n", "---")]
    [TestCase("---\nname: [acme\n---\n# Cook\n", "acme-cook.md")]
    public void AFaultyHeader_RefusesTheFile(string text, string named)
    {
        var refused = Assert.Throws<PromptHeaderException>(() => PromptHeaders.Split(Path, text));

        Assert.That(refused!.Message, Does.Contain(Path).And.Contain(named));
    }

    /// <summary>Two headers are the same when every field is, the toolsets in order.</summary>
    private static bool SameHeader(PromptHeader a, PromptHeader b) =>
        (a.Name, a.Description, a.Level) == (b.Name, b.Description, b.Level) && a.Toolsets.SequenceEqual(b.Toolsets);
}

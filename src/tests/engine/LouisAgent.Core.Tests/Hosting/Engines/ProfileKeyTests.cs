using LouisAgent.Testing;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting.Engines;
using LouisAgent.Core.Prompting;

namespace LouisAgent.Core.Tests.Hosting.Engines;

// The factory's profile key as a value (ADR 0040): its Text is exactly the string the factory built inline before,
// because Profiles and the drop log show it and the factory tests assert it. Written first: ProfileKey didn't exist on
// the red run.
public class ProfileKeyTests
{
    private static ToolsetDefinition[] Toolsets(params string[] names) =>
        names.Select(n =>
                new ToolsetDefinition(n, "a toolset", GuideFile: null, MissingSettings: _ => [], CreateTools: _ => []))
            .ToArray();

    [TestCase(new[] { SampleToolset.Delta, SampleToolset.Epsilon }, null, RoleLevel.Agent, null, null,
        SampleToolset.Delta + "," + SampleToolset.Epsilon)]
    [TestCase(new[] { SampleToolset.Delta }, "testprod/voice-af", RoleLevel.Agent, null, null,
        SampleToolset.Delta + "|testprod/voice-af")]
    [TestCase(new[] { SampleToolset.Delta }, null, RoleLevel.Subagent, null, null,
        SampleToolset.Delta + "|subagent")]
    [TestCase(new[] { SampleToolset.Alpha }, "cook", RoleLevel.Subagent, null, null,
        SampleToolset.Alpha + "|cook|subagent")]
    [TestCase(new[] { SampleToolset.Alpha }, null, RoleLevel.Agent, "ollama-puls", null,
        SampleToolset.Alpha + "|llm:ollama-puls")]
    [TestCase(new[] { SampleToolset.Alpha }, "puls-lookup-reader", RoleLevel.Lookup, null, "ReadPage",
        SampleToolset.Alpha + "|puls-lookup-reader|lookup|tool:ReadPage")]
    [TestCase(new string[0], null, RoleLevel.Agent, null, null, "")]
    public void Text_IsExactlyTheStringTheFactoryBuiltBefore(string[] toolsets, string? role, RoleLevel level,
        string? llm, string? tool, string expected) =>
        Assert.That(ProfileKey.Of(Toolsets(toolsets), role, level, llm, tool).Text, Is.EqualTo(expected));

    [Test]
    public void LevelZero_AddsNothingToTheText_AndEveryOtherLevelAddsItsName()
    {
        Assert.That(ProfileKey.Of(Toolsets(SampleToolset.Alpha), null, RoleLevel.Agent, null, null).Text,
            Is.EqualTo(SampleToolset.Alpha));
        Assert.That(ProfileKey.Of(Toolsets(SampleToolset.Alpha), null, RoleLevel.Subagent, null, null).Text,
            Is.EqualTo(SampleToolset.Alpha + "|subagent"));
        Assert.That(ProfileKey.Of(Toolsets(SampleToolset.Alpha), null, RoleLevel.Lookup, null, null).Text,
            Is.EqualTo(SampleToolset.Alpha + "|lookup"));
    }

    [Test]
    public void EmptyMeansNone_ForTheRoleTheLlmAndTheTool_SoTwoSpellingsOfOneRequestAreOneKey()
    {
        // The factory used to turn "" into null before building the key; the key states the rule once, and the factory
        // reads its parts back.
        var spelledOut = ProfileKey.Of(Toolsets(SampleToolset.Alpha), "", RoleLevel.Subagent, "", "   ");
        var none = ProfileKey.Of(Toolsets(SampleToolset.Alpha), null, RoleLevel.Subagent, null, null);

        Assert.That((spelledOut.Role, spelledOut.Llm, spelledOut.Tool),
            Is.EqualTo(((string?)null, (string?)null, (string?)null)));
        Assert.That(spelledOut, Is.EqualTo(none));
        Assert.That(spelledOut.Text, Is.EqualTo(SampleToolset.Alpha + "|subagent"));
    }

    [Test]
    public void OnlyTheToolTreatsWhitespaceAsNone_AsTheFactoryDid()
    {
        var key = ProfileKey.Of(Toolsets(SampleToolset.Alpha), " ", RoleLevel.Agent, " ", "ReadPage");

        Assert.That((key.Role, key.Llm), Is.EqualTo((" ", " ")),
            "a blank role or LLM name is kept, so it is refused as unknown, not taken for none");
    }

    [Test]
    public void TwoKeysBuiltFromTheSameParts_AreEqual_AndWorkAsOneDictionaryKey()
    {
        var a = ProfileKey.Of(Toolsets(SampleToolset.Delta, SampleToolset.Epsilon), "cook", RoleLevel.Subagent,
            "ollama-puls", "ReadPage");
        var b = ProfileKey.Of(Toolsets(SampleToolset.Delta, SampleToolset.Epsilon), "cook", RoleLevel.Subagent,
            "ollama-puls", "ReadPage");
        var dictionary = new Dictionary<ProfileKey, int> { [a] = 1 };

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(dictionary[b], Is.EqualTo(1));
        Assert.That(ProfileKey.Of(Toolsets(SampleToolset.Delta), null, RoleLevel.Agent, null, null),
            Is.Not.EqualTo(ProfileKey.Of(Toolsets(SampleToolset.Epsilon), null, RoleLevel.Agent, null, null)));
    }
}

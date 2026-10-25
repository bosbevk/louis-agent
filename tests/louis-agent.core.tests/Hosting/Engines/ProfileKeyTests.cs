using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting.Engines;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Hosting.Engines;

// The factory's profile key as a value (ADR 0040): its Text is exactly the string the factory built inline before, because Profiles and the
// drop log show it and the factory tests assert it. Written first: ProfileKey didn't exist on the red run.
public class ProfileKeyTests
{
    private static ToolsetDefinition[] Toolsets(params string[] names) =>
        names.Select(n => new ToolsetDefinition(n, "a toolset", GuideFile: null, MissingSettings: _ => [], CreateTools: _ => [])).ToArray();

    [TestCase(new[] { "files", "git" }, null, RoleLevel.Agent, null, null, "files,git")]
    [TestCase(new[] { "files" }, "testprod/voice-af", RoleLevel.Agent, null, null, "files|testprod/voice-af")]
    [TestCase(new[] { "files" }, null, RoleLevel.Specialist, null, null, "files|specialist")]
    [TestCase(new[] { "web" }, "cook", RoleLevel.Specialist, null, null, "web|cook|specialist")]
    [TestCase(new[] { "web" }, null, RoleLevel.Agent, "ollama-puls", null, "web|llm:ollama-puls")]
    [TestCase(new[] { "web" }, "puls-minion-reader", RoleLevel.Minion, null, "ReadPage", "web|puls-minion-reader|minion|tool:ReadPage")]
    [TestCase(new string[0], null, RoleLevel.Agent, null, null, "")]
    public void Text_IsExactlyTheStringTheFactoryBuiltBefore(string[] toolsets, string? role, RoleLevel level, string? llm, string? tool, string expected) =>
        Assert.That(ProfileKey.Of(Toolsets(toolsets), role, level, llm, tool).Text, Is.EqualTo(expected));

    [Test]
    public void LevelZero_AddsNothingToTheText_AndEveryOtherLevelAddsItsName()
    {
        Assert.That(ProfileKey.Of(Toolsets("web"), null, RoleLevel.Agent, null, null).Text, Is.EqualTo("web"));
        Assert.That(ProfileKey.Of(Toolsets("web"), null, RoleLevel.Specialist, null, null).Text, Is.EqualTo("web|specialist"));
        Assert.That(ProfileKey.Of(Toolsets("web"), null, RoleLevel.Minion, null, null).Text, Is.EqualTo("web|minion"));
    }

    [Test]
    public void TwoKeysBuiltFromTheSameParts_AreEqual_AndWorkAsOneDictionaryKey()
    {
        var a = ProfileKey.Of(Toolsets("files", "git"), "cook", RoleLevel.Specialist, "ollama-puls", "ReadPage");
        var b = ProfileKey.Of(Toolsets("files", "git"), "cook", RoleLevel.Specialist, "ollama-puls", "ReadPage");
        var dictionary = new Dictionary<ProfileKey, int> { [a] = 1 };

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(dictionary[b], Is.EqualTo(1));
        Assert.That(ProfileKey.Of(Toolsets("files"), null, RoleLevel.Agent, null, null), Is.Not.EqualTo(ProfileKey.Of(Toolsets("git"), null, RoleLevel.Agent, null, null)));
    }
}

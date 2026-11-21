using LouisAgent.Web.Shell;

namespace LouisAgent.Web.Tests.Shell;

/// <summary>A model's id reads as a name in the header, and no id reads as nothing.</summary>
public class ModelNamesTests
{
    [TestCase("claude-opus-5-5", "Claude Opus 5.5")]
    [TestCase("claude-haiku-4-5-20251001", "Claude Haiku 4.5.20251001")]
    [TestCase("llama3", "Llama3")]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void AnId_ReadsAsAName(string? id, string? expected) =>
        Assert.That(ModelNames.Pretty(id), Is.EqualTo(expected));
}

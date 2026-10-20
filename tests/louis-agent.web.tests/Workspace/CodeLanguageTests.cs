using LouisAgent.Web.Workspace;

namespace LouisAgent.Web.Tests.Workspace;

// W2b: the highlight.js language for a workspace file, by its name; none means plain text.
public class CodeLanguageTests
{
    [TestCase("Program.cs", "csharp")]
    [TestCase("App.razor", "xml")]
    [TestCase("louis-agent.core.csproj", "xml")]
    [TestCase("script.PY", "python")]
    [TestCase("Dockerfile.puls", "dockerfile")]
    [TestCase("notes.txt", null)]
    [TestCase("LICENSE", null)]
    public void AFileName_PicksItsLanguage(string fileName, string? language) =>
        Assert.That(CodeLanguage.For(fileName), Is.EqualTo(language));
}

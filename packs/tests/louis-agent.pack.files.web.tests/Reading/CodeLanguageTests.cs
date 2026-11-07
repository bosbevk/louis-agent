using LouisAgent.Choices;
using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Web.Api;

namespace LouisAgent.Packs.Files.Web.Tests.Reading;

/// <summary>The highlight.js language for a workspace file, by its name; none means plain text.</summary>
public class CodeLanguageTests
{
    /// <summary>A file's name picks its language.</summary>
    [TestCase("Program.cs", "csharp")]
    [TestCase("App.razor", "xml")]
    [TestCase("louis-agent.core.csproj", "xml")]
    [TestCase("Dockerfile.puls", "dockerfile")]
    [TestCase("notes.txt", null)]
    [TestCase("LICENSE", null)]
    public void AFileName_PicksItsLanguage(string fileName, string? language) =>
        Assert.That(CodeLanguage.For(fileName), Is.EqualTo(language));

    /// <summary>A script's highlight.js id is its language's own name (the browser's copy, ADR 0043).</summary>
    [TestCase("script.PY", ScriptLanguage.Python)]
    [TestCase("run.sh", ScriptLanguage.Bash)]
    [TestCase("build.ps1", ScriptLanguage.PowerShell)]
    public void AScriptFileName_PicksTheScriptLanguage(string fileName, ScriptLanguage language) =>
        Assert.That(CodeLanguage.For(fileName), Is.EqualTo(language.Wire()));
}

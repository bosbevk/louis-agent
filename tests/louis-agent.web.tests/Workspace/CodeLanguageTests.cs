using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Workspace;

namespace LouisAgent.Web.Tests.Workspace;

// W2b: the highlight.js language for a workspace file, by its name; none means plain text.
public class CodeLanguageTests
{
    [TestCase("Program.cs", "csharp")]
    [TestCase("App.razor", "xml")]
    [TestCase("louis-agent.core.csproj", "xml")]
    [TestCase("Dockerfile.puls", "dockerfile")]
    [TestCase("notes.txt", null)]
    [TestCase("LICENSE", null)]
    public void AFileName_PicksItsLanguage(string fileName, string? language) =>
        Assert.That(CodeLanguage.For(fileName), Is.EqualTo(language));

    // A script's highlight.js id is the script language's own name (the browser side's copy of core's, ADR 0043).
    [TestCase("script.PY", ScriptLanguage.Python)]
    [TestCase("run.sh", ScriptLanguage.Bash)]
    [TestCase("build.ps1", ScriptLanguage.PowerShell)]
    public void AScriptFileName_PicksTheScriptLanguage(string fileName, ScriptLanguage language) =>
        Assert.That(CodeLanguage.For(fileName), Is.EqualTo(language.Name()));
}

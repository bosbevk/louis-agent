using System.Text.RegularExpressions;

namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The framework's components call scripts, so the scripts ship with them: a site lists the framework's files, and a
/// component can't call something only one site has.
/// </summary>
public partial class FrameworkScriptsTests
{
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web");

    /// <summary>The account library (CL30), whose components call its own script.</summary>
    private static readonly string Accounts = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.accounts");

    /// <summary>The studio's components (CL57) call the framework's scripts too.</summary>
    private static readonly string Evals = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.evals");

    private static string Script(string name) => File.ReadAllText(Path.Combine(Web, "wwwroot/js", name));

    /// <summary>A script being called by name: matches <c>"chatUi.copy"</c> and gives <c>chatUi.copy</c>.</summary>
    [GeneratedRegex("\"((?:chatUi|louisChat)\\.[A-Za-z]+)\"", RegexOptions.NonBacktracking)]
    private static partial Regex Called();

    /// <summary>A passkey function being called by name: matches <c>"passkeys.create"</c>.</summary>
    [GeneratedRegex("\"(passkeys\\.[A-Za-z]+)\"", RegexOptions.NonBacktracking)]
    private static partial Regex PasskeyCalled();

    /// <summary>Every passkey function the account library's components call is in its own script.</summary>
    [Test]
    public void EveryPasskeyCall_IsInTheLibrarysScript()
    {
        string script = File.ReadAllText(Path.Combine(Accounts, "wwwroot/js/passkeys.js"));
        var called = Directory.EnumerateFiles(Accounts, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => PasskeyCalled().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .Distinct().ToList();

        Assert.That(called, Has.Count.EqualTo(3), "supported, create and get are called");
        Assert.That(called.Where(name => !IsDefined(script, name)), Is.Empty);
    }

    /// <summary>Every script a framework component calls is in the framework's own file.</summary>
    [Test]
    public void EveryScriptTheFrameworkCalls_IsInItsOwnFile()
    {
        string script = Script("chat.js");
        var called = Directory.EnumerateFiles(Web, "*.*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Evals, "*.*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) ||
                           file.EndsWith(".razor", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => Called().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .Distinct().ToList();

        Assert.That(called, Is.Not.Empty, "the components are read");
        Assert.That(called.Where(name => !IsDefined(script, name)), Is.Empty);
    }

    /// <summary>What a browser keeps goes by a neutral name, the same for the first paint and for the settings.</summary>
    [TestCase("chat.js")]
    [TestCase("first-paint.js")]
    public void WhatTheBrowserKeeps_GoesByANeutralName(string file) =>
        Assert.That(Script(file), Does.Contain("\"chat.").And.Not.Contain("\"puls."));

    /// <summary>The script holds no words for the person: it says what went wrong, and the site's words say it.</summary>
    [Test]
    public void TheScript_NamesAProblem_RatherThanSayingIt()
    {
        string script = Script("chat.js");

        Assert.That(script, Does.Contain("\"denied\"").And.Contain("error: \"no-browser\""));
        Assert.That(script, Does.Not.Contain("accept-language=af"), "the place's language is the site's");
    }

    /// <summary>Whether the file sets this function up: a method of its object, one added to it after, or a property of its literal.</summary>
    private static bool IsDefined(string script, string name)
    {
        string method = name[(name.IndexOf('.') + 1)..];
        return script.Contains($"{name} =", StringComparison.Ordinal)
               || script.Contains($"    {method}(", StringComparison.Ordinal)
               || script.Contains($"    {method}:", StringComparison.Ordinal);
    }
}

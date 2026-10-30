using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Manuals;

/// <summary>
/// Each of Puls's evals in C# keeps its manual beside it (<c>evals/louis-agent.evals.puls/Manuals/cook.md</c>), in
/// English, opening with a title, naming the eval and every check it declares in backticks, so a new check can't ship
/// unexplained; the cook's manual names its modes too. The routing eval's manual is beside its file now, tested with
/// it beside the identity.
/// </summary>
public class PulsManualTests
{
    /// <summary>The repository's root, found above where the tests run.</summary>
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>The folder above the tests' output that holds the solution file.</summary>
    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "louis-agent-solution.sln")))
                return directory.FullName;
        throw new InvalidOperationException("the solution's folder was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>The eval's manual's text, read from the repository.</summary>
    private static string Manual(IEval eval) =>
        File.ReadAllText(Path.Combine(RepoRoot, eval.Manual!.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>The set's eval of this name.</summary>
    private static IEval Eval(PulsEvalName name) => new PulsEvalSet().Evals.Single(e => e.Name == name.Wire());

    /// <summary>An eval lists its manual beside itself.</summary>
    [TestCase(PulsEvalName.Cook, "evals/louis-agent.evals.puls/Manuals/cook.md")]
    public void AnEvalListsItsManual_BesideItself(PulsEvalName eval, string path) =>
        Assert.That(Eval(eval).Manual, Is.EqualTo(path));

    /// <summary>A manual opens with a title and names the eval and every check, in backticks.</summary>
    [TestCase(PulsEvalName.Cook)]
    public void TheManual_OpensWithATitle_AndNamesTheEvalAndEveryGrader(PulsEvalName name)
    {
        var eval = Eval(name);
        string text = Manual(eval);

        Assert.That(text, Does.StartWith("# "));
        Assert.That(text, Does.Contain($"`{eval.Name}`"));
        Assert.That(eval.Checks.Select(g => g.Name), Has.All.Matches<string>(grader => text.Contains($"`{grader}`")),
            "every grader, in backticks");
    }

    /// <summary>The cook's manual names each of its modes, in backticks.</summary>
    [Test]
    public async Task TheCooksManual_NamesItsModes()
    {
        var eval = Eval(PulsEvalName.Cook);
        string text = Manual(eval);

        Assert.That((await eval.ModesAsync(null!, CancellationToken.None)).Select(m => m.Name),
            Has.All.Matches<string>(mode => text.Contains($"`{mode}`")));
    }
}

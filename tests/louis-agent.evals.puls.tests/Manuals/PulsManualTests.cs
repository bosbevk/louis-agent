namespace LouisAgent.Evals.Puls.Tests.Manuals;

// F18 step 7.1: each Puls eval's manual lives beside the eval (evals/louis-agent.evals.puls/Manuals/{cook,routing}.md), in English, opens with a title, and names the eval and every grader it declares,
// in backticks, so a new grader can't ship unexplained; the cook's manual names its modes too. Written first: the manuals were benchmarks/{cook,routing}/HANDLEIDING.md, about the Python checks' score names.
public class PulsManualTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "louis-agent-solution.sln"))) return directory.FullName;
        throw new InvalidOperationException("the solution's folder was not found above " + AppContext.BaseDirectory);
    }

    private static string Manual(IEval eval) => File.ReadAllText(Path.Combine(RepoRoot, eval.Manual!.Replace('/', Path.DirectorySeparatorChar)));

    private static IEval Eval(string name) => new PulsEvalSet().Evals.Single(e => e.Name == name);

    [TestCase("puls-cook", "evals/louis-agent.evals.puls/Manuals/cook.md")]
    [TestCase("puls-routing", "evals/louis-agent.evals.puls/Manuals/routing.md")]
    public void AnEvalListsItsManual_BesideItself(string eval, string path) => Assert.That(Eval(eval).Manual, Is.EqualTo(path));

    [TestCase("puls-cook")]
    [TestCase("puls-routing")]
    public void TheManual_OpensWithATitle_AndNamesTheEvalAndEveryGrader(string name)
    {
        var eval = Eval(name);
        string text = Manual(eval);

        Assert.That(text, Does.StartWith("# "));
        Assert.That(text, Does.Contain($"`{eval.Name}`"));
        Assert.That(eval.Graders.Select(g => g.Name), Has.All.Matches<string>(grader => text.Contains($"`{grader}`")), "every grader, in backticks");
    }

    [Test]
    public async Task TheCooksManual_NamesItsModes()
    {
        var eval = Eval("puls-cook");
        string text = Manual(eval);

        Assert.That((await eval.ModesAsync(null!, CancellationToken.None)).Select(m => m.Name), Has.All.Matches<string>(mode => text.Contains($"`{mode}`")));
    }
}

using LouisAgent.Evals.Template.Evals.Example;
using LouisAgent.Choices;
using System.Text.RegularExpressions;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Layout;

/// <summary>
/// The C# evals left, the stub and the template's example, are cut per eval (ADR 0010, 0034): one folder holds
/// everything only that eval uses, with the same names every time, and what two evals or more use is in
/// <c>Shared/</c> and only then. A ratchet in the style of <c>SkillsLayoutTests</c>, read from the source tree, so a
/// misplaced file fails here before anyone has to find it.
/// </summary>
/// <remarks>
/// <code>
///   louis-agent.evals.template/  its csproj and the folders Evals/, Shared/, Messages/, Prompts/, Manuals/ only
///     Evals/{Folder}/            {Folder}Eval.cs, {Folder}Setup.cs (optional), Grading/, Scoring/, Dataset/,
///                                Transcripts/
///     Shared/                    Grading/, Scoring/, Turns/ ... used by two evals or more
/// </code>
/// An eval that is a folder of files (F49.2) is held by its own tests, not here; step 10 makes the template one too.
/// </remarks>
public class EvalLayoutTests
{
    /// <summary>The template's project, the one whose root is held to the rule.</summary>
    private const string TemplateProject = "louis-agent.evals.template";

    /// <summary>A project that holds C# evals: the runner for the stub, and the template.</summary>
    public sealed record EvalProject(string Name, string Folder, string RootNamespace, IReadOnlyList<IEval> Evals);

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

    /// <summary>Every project held to the layout: the runner (for the stub) and the template.</summary>
    private static IEnumerable<EvalProject> Projects()
    {
        yield return ProjectOf(typeof(StubEval).Assembly.GetName().Name!, "LouisAgent.Evals.Runner", new StubEval());

        // The runner doesn't know the template: this project references it for this test alone, and it is held to the
        // layout, so a copy of it starts right and can't drift from the rule.
        yield return ProjectOf(TemplateProject, "LouisAgent.Evals.Template", new ExampleEval());
    }

    /// <summary>A project under <c>evals/</c>, by its assembly's name, with its one eval.</summary>
    private static EvalProject ProjectOf(string assembly, string rootNamespace, IEval eval) =>
        new(assembly, Path.Combine(RepoRoot, "evals", assembly), rootNamespace, [eval]);

    /// <summary>One test case per project, named for it.</summary>
    private static IEnumerable<TestCaseData> EachProject() =>
        Projects().Select(project => new TestCaseData(project).SetName(project.Name));

    /// <summary>The eval's folder name: its type is <c>{Folder}Eval</c>.</summary>
    private static string FolderOf(IEval eval) => Regex.Replace(eval.GetType().Name, "Eval$", "");

    /// <summary>The C# files under a folder, outside build output; none when the folder isn't there.</summary>
    private static IEnumerable<string> SourceFiles(string folder) =>
        Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f))
            : [];

    /// <summary>True for a path under <c>bin/</c> or <c>obj/</c>.</summary>
    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");

    /// <summary>A path as the project's name and its path inside it, with forward slashes.</summary>
    private static string Relative(EvalProject project, string path) =>
        $"{project.Name}/" + Path.GetRelativePath(project.Folder, path).Replace('\\', '/');

    /// <summary>An eval is <c>Evals/{Folder}/{Folder}Eval.cs</c>, in the namespace of that folder.</summary>
    [TestCaseSource(nameof(EachProject))]
    public void EachEval_IsAtEvalsFolderEval_InItsNamespace(EvalProject project)
    {
        var wrong = new List<string>();
        foreach (var eval in project.Evals)
        {
            string folder = FolderOf(eval);
            string file = Path.Combine(project.Folder, "Evals", folder, folder + "Eval.cs");
            if (!File.Exists(file))
                wrong.Add(
                    $"{eval.Name}: its {eval.GetType().Name} is not at {project.Name}/Evals/{folder}/{folder}Eval.cs");
            if (eval.GetType().Namespace != $"{project.RootNamespace}.Evals.{folder}")
                wrong.Add(
                    $"{eval.Name}: its namespace is {eval.GetType().Namespace}, not {project.RootNamespace}.Evals.{folder} (namespace = folder path)");
        }

        Assert.That(wrong, Is.Empty, "An eval is Evals/{Folder}/{Folder}Eval.cs:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>An eval keeps its dataset and its transcripts beside it.</summary>
    [TestCaseSource(nameof(EachProject))]
    public void EachEval_HasItsDatasetAndItsTranscripts(EvalProject project)
    {
        var missing = new List<string>();
        foreach (var eval in project.Evals)
        {
            string root = Path.Combine(project.Folder, "Evals", FolderOf(eval));
            // The stub's scenarios are in code: it proves the protocol, it is not a dataset.
            if (eval.Name != "stub" && !File.Exists(Path.Combine(root, "Dataset", "dataset.json")))
                missing.Add($"{eval.Name}: Evals/{FolderOf(eval)}/Dataset/dataset.json is missing");
            // The template's one exception: git keeps no empty folder, and the conformance rule refuses a stray file
            // such as a README, so it has none yet.
            if (project.Name != TemplateProject && !Directory.Exists(Path.Combine(root, "Transcripts")))
                missing.Add($"{eval.Name}: Evals/{FolderOf(eval)}/Transcripts/ is missing");
        }

        Assert.That(missing, Is.Empty, "An eval keeps its data beside it:\n  " + string.Join("\n  ", missing));
    }

    /// <summary>
    /// A check one eval uses is in its <c>Grading/</c>; one two evals use is in <c>Shared/Grading/</c>; or it is the
    /// harness's.
    /// </summary>
    [TestCaseSource(nameof(EachProject))]
    public void EachGrader_IsInItsEvalsGrading_OrSharedGrading_OrTheHarnesss(EvalProject project)
    {
        var wrong = new List<string>();
        foreach (var eval in project.Evals)
            foreach (var grader in eval.Checks)
            {
                string? ns = grader.GetType().Namespace;
                string own = $"{project.RootNamespace}.Evals.{FolderOf(eval)}.Grading",
                    shared = $"{project.RootNamespace}.Shared.Grading";
                bool ok = In(ns, own) || In(ns, shared) || In(ns, "LouisAgent.Evals.Grading.Common") ||
                          In(ns, "LouisAgent.Evals.Grading.Human");
                if (!ok)
                    wrong.Add(
                        $"{eval.Name}: {grader.GetType().Name} is in {ns}, not {own}, {shared} or the harness's Grading.Common or Grading.Human");
            }

        Assert.That(wrong, Is.Empty,
            "A grader only one eval uses is in its Grading/; one two evals use is in Shared/Grading/:\n  " +
            string.Join("\n  ", wrong));
    }

    /// <summary>True when the namespace is the folder's or one under it.</summary>
    private static bool In(string? ns, string folder) =>
        ns == folder || (ns?.StartsWith(folder + ".", StringComparison.Ordinal) ?? false);

    /// <summary>Every file of <c>Shared/</c> is used by two evals or more.</summary>
    [TestCaseSource(nameof(EachProject))]
    public void EachFileOfShared_IsUsedByTwoEvalsOrMore(EvalProject project)
    {
        string shared = Path.Combine(project.Folder, "Shared");
        var evalFolders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals"))
            : [];
        var texts = evalFolders.ToDictionary(f => Path.GetFileName(f),
            f => string.Join(Environment.NewLine, SourceFiles(f).Select(File.ReadAllText)));
        var files = SourceFiles(shared)
            .ToDictionary(f => f, f => (Text: File.ReadAllText(f), Types: DeclaredTypes(f).ToList()));

        // A file is used by an eval that names one of its types; and by every eval that uses another Shared file that
        // names it (ToolHosts reads the shop table, so the evals that use ToolHosts use the table). A file passes when
        // a type it declares is named by two evals or more, which is how a helper record stays with its file.
        var users = files.ToDictionary(f => f.Key,
            f => texts.Where(e => f.Value.Types.Any(t => Names(e.Value, t))).Select(e => e.Key).ToHashSet());
        for (bool changed = true; changed;)
        {
            changed = false;
            foreach (var (file, (_, types)) in files)
                foreach (var (other, (otherText, _)) in files.Where(o => o.Key != file))
                    if (types.Any(t => Names(otherText, t)) && !users[other].IsSubsetOf(users[file]))
                    {
                        users[file].UnionWith(users[other]);
                        changed = true;
                    }
        }

        var wrong = users.Where(u => u.Value.Count < 2)
            .Select(u =>
                $"{Relative(project, u.Key)} is used by {(u.Value.Count == 0 ? "no eval" : "only " + u.Value.Single())}" +
                (u.Value.Count == 1 ? $": it belongs in Evals/{u.Value.Single()}/" : ""))
            .ToList();
        Assert.That(wrong, Is.Empty,
            "Shared/ holds what two evals or more use, and only then:" + Environment.NewLine + "  " +
            string.Join(Environment.NewLine + "  ", wrong));
    }

    /// <summary>True when the text names the type as a whole word.</summary>
    private static bool Names(string text, string type) => Regex.IsMatch(text, $@"\b{type}\b");

    /// <summary>The names of the types a file declares.</summary>
    private static IEnumerable<string> DeclaredTypes(string file) =>
        Regex.Matches(File.ReadAllText(file), @"\b(?:class|record|enum|interface|struct)\s+(\w+)")
            .Select(m => m.Groups[1].Value).Distinct();

    /// <summary>Dependencies run one way: an eval uses <c>Shared/</c>, <c>Shared/</c> never uses an eval.</summary>
    [TestCaseSource(nameof(EachProject))]
    public void Shared_NamesNoEval(EvalProject project)
    {
        var folders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals")).Select(Path.GetFileName)
            : [];
        var wrong = SourceFiles(Path.Combine(project.Folder, "Shared"))
            .SelectMany(file => folders.Where(folder => Regex.IsMatch(File.ReadAllText(file), $@"\.Evals\.{folder}\b"))
                .Select(folder => $"{Relative(project, file)} names Evals.{folder}"))
            .ToList();
        Assert.That(wrong, Is.Empty, "Shared/ is below the evals, never above:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// The template's root is its csproj, the <c>dotnet new</c> config, and <c>Evals/</c>, <c>Shared/</c>,
    /// <c>Messages/</c>, <c>Prompts/</c> (a judge's prompt, ADR 0011) and <c>Manuals/</c>. The runner's rule is only
    /// that the stub is under <c>Evals/</c>.
    /// </summary>
    [TestCaseSource(nameof(EachProject))]
    public void TheProjectRoot_HoldsOnlyTheProjectAndItsFolders(EvalProject project)
    {
        if (project.Name != TemplateProject) return;
        var allowedFiles = new[] { project.Name + ".csproj" };
        var allowedFolders = new[]
        {
            "Evals", "Shared", "Messages", "Prompts", "Manuals", ".template.config", "bin", "obj"
        };
        var stray = Directory.EnumerateFileSystemEntries(project.Folder)
            .Select(Path.GetFileName)
            .Where(name => Directory.Exists(Path.Combine(project.Folder, name!))
                ? !allowedFolders.Contains(name)
                : !allowedFiles.Contains(name) && !name!.EndsWith(".user", StringComparison.Ordinal))
            .ToList();
        Assert.That(stray, Is.Empty,
            $"{project.Name}'s root holds {string.Join(", ", allowedFiles)} and the folders Evals/, Shared/, Messages/, Prompts/ and Manuals/ only:\n  " +
            string.Join("\n  ", stray));
    }

    /// <summary>
    /// The runner's catalogue, built from the eval folders copied beside it, never holds the template's example, so no
    /// run and no portal ever shows it.
    /// </summary>
    [Test]
    public void TheTemplate_IsNeverInTheCatalogue_SoNoRunAndNoPortalShowsIt()
    {
        var files = EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
            .Select(folder => FileEval.Load(folder.Folder, RunnerChecks.Catalog, folder.Source));

        Assert.That(new EvalCatalog(files, new StubEval()).Evals.Select(e => e.Name),
            Does.Not.Contain(TemplateEvalName.Example.Wire()));
    }

    /// <summary>A folder under <c>Evals/</c> is one eval's.</summary>
    [TestCaseSource(nameof(EachProject))]
    public void EachFolderUnderEvals_IsAnEval(EvalProject project)
    {
        var folders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals")).Select(Path.GetFileName).ToList()
            : [];
        var stray = folders.Where(folder => project.Evals.All(eval => FolderOf(eval) != folder)).ToList();
        Assert.That(stray, Is.Empty,
            "A folder under Evals/ is one eval's:\n  " + string.Join("\n  ", stray));
    }
}

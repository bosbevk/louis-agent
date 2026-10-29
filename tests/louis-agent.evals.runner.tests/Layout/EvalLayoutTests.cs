using LouisAgent.Evals.Template.Evals.Example;
using LouisAgent.Choices;
using System.Text.RegularExpressions;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Evals.Stub;
using LouisAgent.Evals.Template;

namespace LouisAgent.Evals.Runner.Tests.Layout;

// F18 step 5.4b (ADR 0010, 0034): an eval project is cut per eval. One folder holds everything only that eval uses, with the same names every time, and
// what two evals or more use is in Shared/ and only then. Adding, changing or deleting an eval touches one folder. A ratchet in the style of
// SkillsLayoutTests, read from the source tree, so a misplaced file fails here before anyone has to find it.
//
//   louis-agent.evals.{set}/   {Set}EvalSet.cs and the folders Evals/, Shared/, Messages/, Prompts/ (the judges' prompts, F18 step 6.2) and Manuals/ (each eval's manual, step 7.1) only
//     Evals/{Folder}/          {Folder}Eval.cs, {Folder}Setup.cs (optional), Grading/, Scoring/, Dataset/, Transcripts/
//     Shared/                  Grading/, Scoring/, Turns/ ... used by two evals or more
public class EvalLayoutTests
{
    /// <summary>A project that holds evals: the sets the runner knows (and the template's, once 5.4c adds it), and the runner for the stub.</summary>
    public sealed record EvalProject(
        string Name,
        string Folder,
        string RootNamespace,
        bool IsSet,
        IReadOnlyList<IEval> Evals,
        Type? SetType);

    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "louis-agent-solution.sln")))
                return directory.FullName;
        throw new InvalidOperationException("the solution's folder was not found above " + AppContext.BaseDirectory);
    }

    private static IEnumerable<EvalProject> Projects()
    {
        foreach (var set in Enum.GetValues<EvalSetKind>().Select(EvalSets.Create))
            yield return ProjectOf(set.GetType().Assembly.GetName().Name!, set.GetType().Namespace!, isSet: true,
                set.Evals, set.GetType());
        var stub = new StubEval();
        yield return ProjectOf(stub.GetType().Assembly.GetName().Name!, "LouisAgent.Evals.Runner", isSet: false, [stub],
            null);

        // The template (F18 step 5.4c) is never registered, so the runner doesn't know it: this project references it for this test alone, and it is
        // held to the layout like any set, so a copy of it starts right and the template can't drift from the rule.
        var template = new TemplateEvalSet();
        yield return ProjectOf(template.GetType().Assembly.GetName().Name!, template.GetType().Namespace!, isSet: true,
            template.Evals, template.GetType());
    }

    private static EvalProject ProjectOf(string assembly, string rootNamespace, bool isSet, IReadOnlyList<IEval> evals,
        Type? setType) =>
        new(assembly, Path.Combine(RepoRoot, "evals", assembly), rootNamespace, isSet, evals, setType);

    private static IEnumerable<TestCaseData> EachProject() =>
        Projects().Select(project => new TestCaseData(project).SetName(project.Name));

    /// <summary>The eval's folder name: its type is <c>{Folder}Eval</c>.</summary>
    private static string FolderOf(IEval eval) => Regex.Replace(eval.GetType().Name, "Eval$", "");

    private static IEnumerable<string> SourceFiles(string folder) =>
        Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f))
            : [];

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");

    private static string Relative(EvalProject project, string path) =>
        $"{project.Name}/" + Path.GetRelativePath(project.Folder, path).Replace('\\', '/');

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
            // The template's one exception: git keeps no empty folder, and the conformance rule refuses a stray file such as a README, so it has none yet.
            if (project.Name != "louis-agent.evals.template" && !Directory.Exists(Path.Combine(root, "Transcripts")))
                missing.Add($"{eval.Name}: Evals/{FolderOf(eval)}/Transcripts/ is missing");
        }

        Assert.That(missing, Is.Empty, "An eval keeps its data beside it:\n  " + string.Join("\n  ", missing));
    }

    [TestCaseSource(nameof(EachProject))]
    public void EachGrader_IsInItsEvalsGrading_OrSharedGrading_OrTheHarnesss(EvalProject project)
    {
        var wrong = new List<string>();
        foreach (var eval in project.Evals)
            foreach (var grader in eval.Graders)
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

    private static bool In(string? ns, string folder) =>
        ns == folder || (ns?.StartsWith(folder + ".", StringComparison.Ordinal) ?? false);

    [TestCaseSource(nameof(EachProject))]
    public void EachFileOfShared_IsUsedByTwoEvalsOrMore(EvalProject project)
    {
        string shared = Path.Combine(project.Folder, "Shared");
        var evalFolders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals"))
            : [];
        var texts = evalFolders.ToDictionary(f => Path.GetFileName(f)!,
            f => string.Join(Environment.NewLine, SourceFiles(f).Select(File.ReadAllText)));
        var files = SourceFiles(shared)
            .ToDictionary(f => f, f => (Text: File.ReadAllText(f), Types: DeclaredTypes(f).ToList()));

        // A file is used by an eval that names one of its types; and by every eval that uses another Shared file that names it (ToolHosts reads the
        // shop table, so the evals that use ToolHosts use the table). A file passes when a type it declares is named by two evals or more, which
        // is how a helper record stays with its file.
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

    private static bool Names(string text, string type) => Regex.IsMatch(text, $@"\b{type}\b");

    private static IEnumerable<string> DeclaredTypes(string file) =>
        Regex.Matches(File.ReadAllText(file), @"\b(?:class|record|enum|interface|struct)\s+(\w+)")
            .Select(m => m.Groups[1].Value).Distinct();

    [TestCaseSource(nameof(EachProject))]
    public void Shared_NamesNoEval(EvalProject project)
    {
        // Dependencies run one way: an eval uses Shared/, Shared/ never uses an eval.
        var folders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals")).Select(Path.GetFileName)
            : [];
        var wrong = SourceFiles(Path.Combine(project.Folder, "Shared"))
            .SelectMany(file => folders.Where(folder => Regex.IsMatch(File.ReadAllText(file), $@"\.Evals\.{folder}\b"))
                .Select(folder => $"{Relative(project, file)} names Evals.{folder}"))
            .ToList();
        Assert.That(wrong, Is.Empty, "Shared/ is below the evals, never above:\n  " + string.Join("\n  ", wrong));
    }

    [TestCaseSource(nameof(EachProject))]
    public void TheProjectRoot_HoldsOnlyTheSetAndItsFolders(EvalProject project)
    {
        // A set's root is the set class, the csproj, and Evals/, Shared/, Messages/, Prompts/ (a judge's prompt, ADR 0011) and Manuals/ (an eval's manual, F18 step 7.1). The runner is not a set: its rule is only that the stub is under Evals/.
        if (!project.IsSet) return;
        var allowedFiles = new[] { project.SetType!.Name + ".cs", project.Name + ".csproj" };
        // The template alone also holds the `dotnet new` template's config (.template.config); a real set doesn't.
        var allowedFolders = new[] { "Evals", "Shared", "Messages", "Prompts", "Manuals", "bin", "obj" }
            .Concat(project.Name == "louis-agent.evals.template" ? new[] { ".template.config" } : []).ToArray();
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

    [Test]
    public void TheTemplate_IsNeverRegistered_SoNoRunAndNoPortalShowsIt()
    {
        Assert.That(Enum.GetNames<EvalSetKind>(), Does.Not.Contain("Template"),
            "EvalSetKind doesn't list the template");
        Assert.That(
            new EvalCatalog(Enum.GetValues<EvalSetKind>().Select(EvalSets.Create).ToList(), new StubEval()).Evals
                .Select(e => e.Name),
            Does.Not.Contain(TemplateEvalName.Example.Wire()));
    }

    [TestCaseSource(nameof(EachProject))]
    public void EachFolderUnderEvals_IsAnEval(EvalProject project)
    {
        var folders = Directory.Exists(Path.Combine(project.Folder, "Evals"))
            ? Directory.GetDirectories(Path.Combine(project.Folder, "Evals")).Select(Path.GetFileName).ToList()
            : [];
        var stray = folders.Where(folder => project.Evals.All(eval => FolderOf(eval) != folder)).ToList();
        Assert.That(stray, Is.Empty,
            "A folder under Evals/ is one eval's, listed by the set:\n  " + string.Join("\n  ", stray));
    }
}

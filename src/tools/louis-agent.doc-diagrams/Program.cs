using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.DocDiagrams;
using LouisAgent.DocDiagrams.Messages;

// Draws every doc's diagram (CL60.1): each diagrams/*.json under the framework's docs, each demo's and the packs'
// becomes the SVG beside it, written only when it changed. Run it after changing a diagram, and commit what it
// writes; --check writes nothing and fails naming a stale one, as the docs' test does.
//
//   dotnet run --project src/tools/louis-agent.doc-diagrams [--check]
const string checkOption = "--check";

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
bool check = args.Contains(checkOption);
string repoRoot = FindRepoRoot();

var counts = Enum.GetValues<SyncOutcome>().ToDictionary(outcome => outcome, _ => 0);
int failed = 0;
foreach (string json in DiagramSync.Find(DiagramSync.Roots(repoRoot)))
{
    string relative = Path.GetRelativePath(repoRoot, json).Replace('\\', '/');
    try
    {
        var outcome = DiagramSync.Sync(json, check);
        counts[outcome]++;
        if (outcome == SyncOutcome.Written)
            Console.WriteLine(MessageText.Format(DocDiagramsUserText.Written,
                new { Path = Path.ChangeExtension(relative, ".svg") }));
        if (outcome == SyncOutcome.Stale)
            Console.Error.WriteLine(MessageText.Format(DocDiagramsUserText.Stale, new { Path = relative }));
    }
    catch (InvalidDataException ex)
    {
        failed++;
        Console.Error.WriteLine(MessageText.Format(DocDiagramsUserText.Unreadable,
            new
            {
                Path = relative,
                Reason = ex.Message
            }));
    }
}

Console.WriteLine(MessageText.Format(DocDiagramsUserText.Summary,
    new
    {
        Written = counts[SyncOutcome.Written],
        Unchanged = counts[SyncOutcome.Unchanged],
        Stale = counts[SyncOutcome.Stale],
        Failed = failed
    }));
return counts[SyncOutcome.Stale] + failed == 0 ? 0 : 1;

// The folder holding the solution, found upward from where the tool runs.
static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (!File.Exists(Path.Combine(dir.FullName, "louis-agent-solution.sln"))) dir = dir.Parent!;
    return dir.FullName;
}

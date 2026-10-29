using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Layout;

// F18 step 5.4b, a learning test: how the one data item of an eval project (Update="Evals\**\Dataset\**\*;Evals\**\Transcripts\**\*" with a TargetPath of
// EvalFiles\$(AssemblyName)\%(RecursiveDir)...) behaves, before anything relies on it. What it showed:
//  - %(RecursiveDir) is the file's whole path from the project root, because the default None glob starts there, so the files land in
//    EvalFiles/{assembly}/Evals/{Folder}/Dataset/... (not .../{Folder}/Dataset/...), and EvalFiles says so;
//  - the files reach the runner's output and this project's output through the ProjectReference alone: neither project lists them;
//  - TargetPath, not Link, because Link shows every file a second time in the IDE, under a folder named like the project.
public class EvalFilesCopyTests
{
    private const string CookDataset = "EvalFiles/louis-agent.evals.puls/Evals/Cook/Dataset/dataset.json";

    private const string StubReference =
        "EvalFiles/louis-agent.evals.runner/Evals/Stub/Transcripts/bobotie.reference.txt";

    [Test]
    public void ADatasetReachesThisProjectsOutput_ThroughTheProjectReference() =>
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, CookDataset)), Is.True, CookDataset);

    [Test]
    public void TheStubsTranscripts_ReachThisProjectsOutput_FromTheRunnersOwnItem() =>
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, StubReference)), Is.True, StubReference);

    [Test]
    public void ADatasetReachesTheRunnersOwnOutput_ThroughItsReferenceToThePulsProject()
    {
        // This project's output is bin/{Config}/net10.0/; the runner's is the same under its own folder.
        var output =
            new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
        string runnerOutput = Path.Combine(output.Parent!.Parent!.Parent!.Parent!.Parent!.FullName, "evals",
            "louis-agent.evals.runner", "bin", output.Parent.Name, output.Name);

        Assert.That(File.Exists(Path.Combine(runnerOutput, CookDataset)), Is.True,
            Path.Combine(runnerOutput, CookDataset));
    }

    [Test]
    public void EvalFiles_SaysWhereTheItemPutThem()
    {
        Assert.That(EvalFiles.DatasetOf(typeof(CookEval)),
            Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "EvalFiles", "louis-agent.evals.puls", "Evals", "Cook",
                "Dataset")));
        Assert.That(EvalFiles.TranscriptsOf(typeof(StubEval)),
            Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "EvalFiles", "louis-agent.evals.runner", "Evals", "Stub",
                "Transcripts")));
        Assert.That(File.Exists(Path.Combine(EvalFiles.DatasetOf(typeof(CookEval)), "dataset.json")), Is.True);
    }
}

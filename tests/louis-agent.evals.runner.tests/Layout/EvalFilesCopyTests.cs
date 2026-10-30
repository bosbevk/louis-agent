using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Layout;

/// <summary>
/// How the one data item of an eval project copies its eval files: every file under <c>Evals/</c> that isn't code
/// lands at <c>EvalFiles/{the project's path from the repository root}/Evals/...</c> beside the assembly, so a copy's
/// path below <c>EvalFiles/</c> is where it sits in the repository (F49.2), which an eval in a file names its manual
/// from.
/// </summary>
/// <remarks>
/// A learning test (F18 step 5.4b) kept as the item's proof: <c>%(RecursiveDir)</c> is the file's whole path from the
/// project root, because the default None glob starts there; the files reach the runner's output and this project's
/// output through the project reference alone; <c>TargetPath</c>, not <c>Link</c>, because <c>Link</c> shows every file
/// a second time in the IDE, under a folder named like the project.
/// </remarks>
public class EvalFilesCopyTests
{
    /// <summary>The cook's manifest, copied from the Puls evals project.</summary>
    private const string CookDataset = "EvalFiles/evals/louis-agent.evals.puls/Evals/Cook/Dataset/dataset.json";

    /// <summary>One of the stub's reference transcripts, copied from the runner's own project.</summary>
    private const string StubReference =
        "EvalFiles/evals/louis-agent.evals.runner/Evals/Stub/Transcripts/bobotie.reference.txt";

    /// <summary>A dataset reaches this project's output through its reference to the Puls project.</summary>
    [Test]
    public void ADatasetReachesThisProjectsOutput_ThroughTheProjectReference() =>
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, CookDataset)), Is.True, CookDataset);

    /// <summary>The stub's transcripts reach it from the runner's own item.</summary>
    [Test]
    public void TheStubsTranscripts_ReachThisProjectsOutput_FromTheRunnersOwnItem() =>
        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, StubReference)), Is.True, StubReference);

    /// <summary>A dataset reaches the runner's own output too.</summary>
    [Test]
    public void ADatasetReachesTheRunnersOwnOutput_ThroughItsReferenceToThePulsProject()
    {
        // This project's output is bin/{Config}/net10.0/; the runner's is the same under its own folder.
        var output =
            new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
        var runnerOutput = Path.Combine(output.Parent!.Parent!.Parent!.Parent!.Parent!.FullName, "evals",
            "louis-agent.evals.runner", "bin", output.Parent.Name, output.Name);

        Assert.That(File.Exists(Path.Combine(runnerOutput, CookDataset)), Is.True,
            Path.Combine(runnerOutput, CookDataset));
    }

    /// <summary>EvalFiles says where the item put an eval in C#'s data.</summary>
    [Test]
    public void EvalFiles_SaysWhereTheItemPutThem()
    {
        Assert.That(EvalFiles.DatasetOf(typeof(CookEval)),
            Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "EvalFiles", "evals", "louis-agent.evals.puls", "Evals",
                "Cook", "Dataset")));
        Assert.That(EvalFiles.TranscriptsOf(typeof(StubEval)),
            Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "EvalFiles", "evals", "louis-agent.evals.runner", "Evals",
                "Stub", "Transcripts")));
        Assert.That(File.Exists(Path.Combine(EvalFiles.DatasetOf(typeof(CookEval)), "dataset.json")), Is.True);
    }
}

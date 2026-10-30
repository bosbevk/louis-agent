namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Where an eval in C#'s data is at run time, so no eval builds a path. An eval keeps its files beside it, in
/// <c>Evals/{Folder}/Dataset</c> and <c>Evals/{Folder}/Transcripts</c>; its project's one data item copies them to
/// <c>EvalFiles/evals/{project}/Evals/{Folder}/...</c> beside the assembly (the project's path from the repository
/// root, F49.2), and that reaches every project that references it (the runner, the tests). The folder is the last
/// part of the eval type's namespace (<c>LouisAgent.Evals.Runner.Evals.Stub</c> is <c>Stub</c>). Since F49.2 only the
/// stub is an eval in C#; an eval that is a folder of files knows its own folder.
/// </summary>
/// <remarks>Every eval project in C# is under <c>evals/</c>, named as its assembly.</remarks>
public static class EvalFiles
{
    /// <summary>The eval's dataset folder.</summary>
    public static string DatasetOf(IEval eval) => DatasetOf(eval.GetType());

    /// <summary>The dataset folder of the eval of this type.</summary>
    public static string DatasetOf(Type eval) => Path.Combine(Root(eval), "Dataset");

    /// <summary>The eval's reference and known-bad transcripts.</summary>
    public static string TranscriptsOf(IEval eval) => TranscriptsOf(eval.GetType());

    /// <summary>The transcripts folder of the eval of this type.</summary>
    public static string TranscriptsOf(Type eval) => Path.Combine(Root(eval), "Transcripts");

    /// <summary>The copy of the eval's own folder.</summary>
    private static string Root(Type eval)
    {
        var folder = (eval.Namespace ?? "").Split('.')[^1];
        return Path.Combine(AppContext.BaseDirectory, "EvalFiles", "evals", eval.Assembly.GetName().Name!, "Evals",
            folder);
    }
}

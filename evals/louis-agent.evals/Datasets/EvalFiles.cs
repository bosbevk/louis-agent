namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Where an eval's data is at run time (F18 step 5.4b), so no eval builds a path. An eval keeps its files beside it, in <c>Evals/{Folder}/Dataset</c> and
/// <c>Evals/{Folder}/Transcripts</c>; its project's one data item copies them to <c>EvalFiles/{assembly}/Evals/{Folder}/...</c> beside the assembly, and
/// that reaches every project that references it (the runner, the tests). The assembly is in the path because two sets may both have a <c>Cook</c>; the
/// folder is the last part of the eval type's namespace (<c>LouisAgent.Evals.Puls.Evals.Cook</c> is <c>Cook</c>).
/// </summary>
public static class EvalFiles
{
    public static string DatasetOf(IEval eval) => DatasetOf(eval.GetType());

    public static string DatasetOf(Type eval) => Path.Combine(Root(eval), "Dataset");

    public static string TranscriptsOf(IEval eval) => TranscriptsOf(eval.GetType());

    public static string TranscriptsOf(Type eval) => Path.Combine(Root(eval), "Transcripts");

    private static string Root(Type eval)
    {
        string folder = (eval.Namespace ?? "").Split('.')[^1];
        return Path.Combine(AppContext.BaseDirectory, "EvalFiles", eval.Assembly.GetName().Name!, "Evals", folder);
    }
}

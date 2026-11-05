namespace LouisAgent.Api.Evals.Runs.Kinds;

/// <summary>Where a run is: still going, finished fine, or failed.</summary>
public enum EvalRunStatus
{
    Running,

    /// <summary>The script exited 0; its folder holds the results.</summary>
    Done,

    /// <summary>The script exited with an error, or couldn't start; its lines say why.</summary>
    Failed,
}

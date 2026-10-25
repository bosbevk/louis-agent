namespace LouisAgent.Evals.Puls.Shared;

/// <summary>
/// What a Puls mode can do (F18 step 5.4b): it can hand a question on when it has the specialists toolset (the Python's rule), whatever the mode is
/// named. The cook, the routing and the route grader all ask it, so it is here and not in either eval.
/// </summary>
public static class PulsModes
{
    public static bool CanDelegate(EvalMode mode) => mode.Toolsets.Contains("specialists", StringComparer.Ordinal);
}

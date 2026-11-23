using LouisAgent.Evals.Contracts.Runs;

namespace LouisAgent.Evals.Contracts.Api;

/// <summary>One eval this host has: its name, the agent it proves, how many conversations, and its last run.</summary>
/// <param name="Name">The eval's name, its file's name without <c>.eval.json</c>.</param>
/// <param name="Agent">The address of the agent it proves.</param>
/// <param name="Conversations">How many conversations its file holds.</param>
/// <param name="LastRun">Its newest run in short, or null before its first.</param>
public sealed record EvalRow(string Name, string Agent, int Conversations, RunRow? LastRun)
{
    /// <summary>
    /// Its last run finished and every one of its conversations passed. A method, so the API's answer gains no field.
    /// </summary>
    public bool LastRunPassedInFull() =>
        LastRun is { Status: RunStatus.Finished } run && run.Passed == Conversations;
}

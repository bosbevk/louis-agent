namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>
/// Who took a step, in plain words (F106.3.4), by the agent's name as the run file writes it: the workflow for the
/// code's own steps, then the sorter, the manager, the investigator, the coder and the reviewer.
/// </summary>
/// <remarks>A switch on the name, one arm per agent (ADR 0051), the words from <see cref="TriageWords"/>.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class AgentWords
{
    /// <summary>
    /// The agents a stage draws, in the order the case meets them; the manager is the hub between them.
    /// </summary>
    public static IReadOnlyList<string> Stage { get; } =
        ["code", "sorter", "manager", "investigator", "coder", "reviewer"];

    /// <summary>The agent's name for a visitor; an agent this demo doesn't know keeps its own name.</summary>
    public static string Of(string agent, TriageWords words) => agent switch
    {
        "code" => words.Workflow,
        "sorter" => words.Sorter,
        "manager" => words.Manager,
        "investigator" => words.Investigator,
        "coder" => words.Coder,
        "reviewer" => words.Reviewer,
        _ => agent,
    };
}

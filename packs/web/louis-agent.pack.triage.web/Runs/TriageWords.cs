namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>
/// The demo's words for each kind of step (F106.3.4): what that kind of step is, in plain words, shown beside what
/// this one did. English by default; a site in another language gives its own, as it does the chat's words.
/// </summary>
public sealed record TriageWords
{
    /// <summary>A step of a kind this demo doesn't know yet.</summary>
    public string Unknown { get; init; } = "A step of the triage.";

    /// <summary>The workflow read the error and opened a case.</summary>
    public string Collected { get; init; } = "The workflow reads the error from the service's log and opens a case.";

    /// <summary>The sorter routed it.</summary>
    public string Sorted { get; init; } = "The sorter reads the error and says which department it belongs to.";

    /// <summary>The manager decided.</summary>
    public string Decided { get; init; } =
        "The manager decides what the case gets: look into it, fix it, leave it, or give it to a person.";

    /// <summary>The investigator reported.</summary>
    public string Investigated { get; init; } =
        "The investigator reads the code behind the error and reports where, why, whose area it is and how sure.";

    /// <summary>The coder fixed it.</summary>
    public string Fixed { get; init; } = "The coder fixes the cause on a branch of its own, with a test.";

    /// <summary>The code checked the fix.</summary>
    public string Checked { get; init; } =
        "The code checks the fix itself: the commit on the branch, the tests, and the request that failed.";

    /// <summary>The reviewer judged it.</summary>
    public string Reviewed { get; init; } =
        "The reviewer holds the fix against the cause and says whether a careful colleague would keep it.";

    /// <summary>A person got the case.</summary>
    public string Raised { get; init; } = "The case goes to a person, with everything found so far.";

    /// <summary>Another department got the case.</summary>
    public string HandedOff { get; init; } = "The case moves to another department, with its whole history.";

    /// <summary>The case closed.</summary>
    public string Finished { get; init; } = "The case is closed. Nothing is ever pushed: a person merges the fix.";

    /// <summary>The code between the agents: it reads the log, commits, checks and closes.</summary>
    public string Workflow { get; init; } = "The workflow";

    /// <summary>The agent that routes an error to its department.</summary>
    public string Sorter { get; init; } = "The sorter";

    /// <summary>The hub that decides and checks everyone.</summary>
    public string Manager { get; init; } = "The manager";

    /// <summary>The agent that finds the cause.</summary>
    public string Investigator { get; init; } = "The investigator";

    /// <summary>The coding agent that fixes it.</summary>
    public string Coder { get; init; } = "The coder";

    /// <summary>The agent that judges the fix.</summary>
    public string Reviewer { get; init; } = "The reviewer";

    /// <summary>The control that shows the step before.</summary>
    public string Back { get; init; } = "Back";

    /// <summary>The control that shows the next step.</summary>
    public string Next { get; init; } = "Next";

    /// <summary>The control that plays the run from the start.</summary>
    public string PlayAgain { get; init; } = "Play it again";

    /// <summary>What the step counter says before the number.</summary>
    public string Step { get; init; } = "step";
}

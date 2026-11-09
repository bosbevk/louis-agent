namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>
/// The plain words for a kind of step (F106.3.4), by the kind's name as the run file writes it: what the sorter, the
/// manager, the investigator, the coder, the code and the reviewer each do, for a visitor who has never seen a triage.
/// </summary>
/// <remarks>
/// A switch on the kind's name, one arm per kind read like a table (ADR 0051), the words from <see cref="TriageWords"/>
/// so a site can give its own; a kind this demo doesn't know gets neutral words, never an error on the page.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class StepWords
{
    /// <summary>The words for a kind of step, in the default English.</summary>
    public static string Of(string kind) => Of(kind, new TriageWords());

    /// <summary>The words for a kind of step, from the site's words.</summary>
    public static string Of(string kind, TriageWords words) => kind switch
    {
        "collected" => words.Collected,
        "sorted" => words.Sorted,
        "decided" => words.Decided,
        "investigated" => words.Investigated,
        "fixed" => words.Fixed,
        "checked" => words.Checked,
        "reviewed" => words.Reviewed,
        "raised" => words.Raised,
        "handed-off" => words.HandedOff,
        "finished" => words.Finished,
        _ => words.Unknown,
    };
}

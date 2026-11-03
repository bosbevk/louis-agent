namespace LouisAgent.Puls.Admin.Studio;

/// <summary>
/// The studio's sentences, out of its code-behind as <c>ReviewText</c> is: what the sample dialog and the keep buttons
/// say when something was kept, or can't be.
/// </summary>
public static class StudioText
{
    /// <summary>Expected values that aren't a JSON object never leave the page.</summary>
    public const string ExpectedNotAnObject =
        "The expected values must be a JSON object, such as {\"serves\": 6}: fix it and make the sample again.";

    /// <summary>Without a name nobody can say who made the draft.</summary>
    public const string NeedsAName = "Type your name in a review first: a draft keeps who made it.";

    /// <summary>How many drafts an eval has, in words.</summary>
    public static string Drafts(int count) => count == 1 ? "1 draft" : $"{count} drafts";

    /// <summary>A sample was kept as a draft of its eval.</summary>
    public static string SampleKept(string sample, string eval) =>
        $"Kept {sample} as a draft of {eval}. It waits on the Evals page until it is published.";

    /// <summary>An eval being composed was saved as a draft (F49.5).</summary>
    public static string EvalSaved(string eval) => $"Saved {eval} as a draft.";

    /// <summary>A try started; the Benchmarks page follows it.</summary>
    public static string TryStarted(string run) =>
        $"Try {run} started, a replay at no cost: follow it on Benchmarks ›";

    /// <summary>A transcript was kept as a draft, under the file name it will have.</summary>
    public static string TranscriptKept(string file) =>
        $"Kept as {file}. It waits on the Evals page until it is published.";
}

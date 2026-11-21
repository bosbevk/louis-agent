using System.Globalization;

namespace LouisAgent.Web.Evals.Admin.Studio;

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

    /// <summary>An eval was committed on its branch; the push and the pull request are the person's.</summary>
    public static string Published(string branch, string folder) =>
        $"Committed {folder} on {branch}. Push the branch and open a pull request to make it part of the evals.";

    /// <summary>An eval was handed back as a zip, where the API has no git workspace.</summary>
    public static string PublishedAsZip(string file) =>
        $"This instance has no git workspace, so the eval came as {file}: add its folder in a pull request.";

    /// <summary>A try started; its run page follows it.</summary>
    public static string TryStarted(string run) =>
        $"Try {run} started, a replay at no cost: open its run ›";

    /// <summary>A run the try can replay, as its picker shows it: whose, when it started (UTC) and its id.</summary>
    public static string ReplayLabel(string eval, DateTime startedUtc, string run) =>
        string.Create(CultureInfo.InvariantCulture, $"{eval} · {startedUtc:yyyy-MM-dd HH:mm} · {run}");

    /// <summary>The picker's first line, before a run is picked.</summary>
    public const string PickARun = "Pick a recorded run to replay";

    /// <summary>No run recorded its answers yet, so there is nothing to replay.</summary>
    public const string NoRunToReplay =
        "No run has recorded its answers yet: run an eval with recording on, then try the draft on it.";

    /// <summary>A draft went out as a promptfoo file.</summary>
    public static string PromptfooExported(string file) =>
        $"Saved the draft, and its promptfoo file came as {file}: run it with promptfoo eval.";

    /// <summary>A transcript was kept as a draft, under the file name it will have.</summary>
    public static string TranscriptKept(string file) =>
        $"Kept as {file}. It waits on the Evals page until it is published.";
}

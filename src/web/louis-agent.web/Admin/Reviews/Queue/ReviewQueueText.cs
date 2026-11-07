namespace LouisAgent.Web.Admin.Reviews.Queue;

/// <summary>
/// The Review page's words: where the open answer sits in the queue, the progress, and nothing waiting.
/// </summary>
/// <remarks>
/// Out of the page's code-behind, beside <c>ReviewText</c>, so the words have tests and the page only places them.
/// </remarks>
public static class ReviewQueueText
{
    /// <summary>"2 of 5": the open answer's place in the whole queue.</summary>
    public static string Place(ReviewQueueView view) => $"{view.Open + 1} of {view.Total}";

    /// <summary>"1 graded · 4 left".</summary>
    public static string Progress(ReviewQueueView view) => $"{view.Graded} graded · {view.Left} left";

    /// <summary>Nothing waits: for this reviewer by name, or for anyone when no name is kept.</summary>
    public static string Empty(string? reviewer) =>
        reviewer is null ? "Nothing waits for a grade." : $"Nothing waits for your grade, {reviewer}.";
}

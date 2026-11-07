namespace LouisAgent.Packs.Git.Web.Tests;

/// <summary>The prompts the page sends when a viewer asks for a review, each with the chat title it gets.</summary>
public class ReviewActionsTests
{
    /// <summary>A diff's review says whether it's staged, uncommitted or a new file.</summary>
    [TestCase(true, false, "staged changes to")]
    [TestCase(false, false, "uncommitted changes to")]
    [TestCase(false, true, "new, untracked file")]
    public void ADiffReview_SaysWhichChanges(bool staged, bool untracked, string which)
    {
        var (prompt, title) = ReviewActions.Diff("docs/a.md", staged, untracked);

        Assert.That(prompt, Does.StartWith($"Review the {which} `docs/a.md`."));
        Assert.That(title, Is.EqualTo("Review changes a.md"));
    }

    /// <summary>Reviewing everything asks for the status and both kinds of diff.</summary>
    [Test]
    public void ReviewingEverything_AsksForTheStatusAndBothDiffs()
    {
        var (prompt, title) = ReviewActions.AllChanges();

        Assert.That(prompt, Does.Contain("staged and unstaged diffs").And.Contain("ready to commit"));
        Assert.That(title, Is.EqualTo("Review all changes"));
    }
}

using LouisAgent.Web.Git;

namespace LouisAgent.Web.Tests.Git;

// W2b: the prompts the page sends when a review is asked for from a viewer, each with the chat title it gets.
public class ReviewActionsTests
{
    [Test]
    public void AFileReview_NamesTheFile_AndTitlesTheChatByItsName()
    {
        var (prompt, title) = ReviewActions.File("src/louis-agent.web/Pages/ChatPage.razor");

        Assert.That(prompt, Does.StartWith("Review `src/louis-agent.web/Pages/ChatPage.razor` from the workspace."));
        Assert.That(title, Is.EqualTo("Review ChatPage.razor"));
    }

    [TestCase(true, false, "staged changes to")]
    [TestCase(false, false, "uncommitted changes to")]
    [TestCase(false, true, "new, untracked file")]
    public void ADiffReview_SaysWhichChanges(bool staged, bool untracked, string which)
    {
        var (prompt, title) = ReviewActions.Diff("docs/a.md", staged, untracked);

        Assert.That(prompt, Does.StartWith($"Review the {which} `docs/a.md`."));
        Assert.That(title, Is.EqualTo("Review changes a.md"));
    }

    [Test]
    public void ReviewingEverything_AsksForTheStatusAndBothDiffs()
    {
        var (prompt, title) = ReviewActions.AllChanges();

        Assert.That(prompt, Does.Contain("staged and unstaged diffs").And.Contain("ready to commit"));
        Assert.That(title, Is.EqualTo("Review all changes"));
    }
}

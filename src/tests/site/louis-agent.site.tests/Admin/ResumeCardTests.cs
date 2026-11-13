using LouisAgent.Site.Admin;
using LouisAgent.Site.Content;

namespace LouisAgent.Site.Tests.Admin;

/// <summary>
/// F142: the Resume area's card on the admin's home. It says whose CV the site holds and how much is in it, from the
/// same copy the page shows, and opens the editor.
/// </summary>
public class ResumeCardTests
{
    /// <summary>The card names the CV, counts its parts, and leads to the editor.</summary>
    [Test]
    public void TheCard_SaysWhatTheCvHolds_AndOpensTheEditor()
    {
        var api = new CvApi(Cv.Resume);
        using var ctx = api.Context();

        var card = ctx.Render<ResumeCard>();
        card.WaitForAssertion(() => Assert.That(card.FindAll(".figure"), Has.Count.EqualTo(3)),
            TimeSpan.FromSeconds(10));

        string body = card.Find(".admin-card-body").TextContent;
        Assert.Multiple(() =>
        {
            Assert.That(card.Find(".admin-card-title").TextContent, Is.EqualTo("Resume"));
            Assert.That(body, Does.Contain("Louis Marais"));
            Assert.That(body, Does.Contain($"{Cv.Resume.Work.Count}").And.Contain("jobs"));
            Assert.That(body, Does.Contain("skill groups").And.Contain("certificates"));
            Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo("admin/resume"));
        });
    }
}

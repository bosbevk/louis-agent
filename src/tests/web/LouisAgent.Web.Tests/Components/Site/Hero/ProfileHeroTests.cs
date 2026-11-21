using LouisAgent.Testing;
using LouisAgent.Web.Components.Site.Hero;

namespace LouisAgent.Web.Tests.Components.Site.Hero;

/// <summary>
/// A person's hero is the framework's: the name, the summary, a quiet line, the ways to reach them and the photo,
/// and on paper a row of its own under both columns in the buttons' place.
/// </summary>
public class ProfileHeroTests
{
    /// <summary>The framework's web project, where the hero's styles are.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web");

    /// <summary>
    /// Every slot lands in its place: the words in one column, the photo beside them, the paper row after.
    /// </summary>
    [Test]
    public void TheHero_DrawsItsSlots()
    {
        using var ctx = new BunitContext();

        var hero = ctx.Render<ProfileHero>(parameters => parameters
            .Add(h => h.Name, "Ada Lovelace")
            .Add(h => h.Summary, "Writes programs.")
            .Add(h => h.Note, "London · English")
            .Add(h => h.Actions, "<a class=\"btn\" href=\"/contact\">Contact</a>")
            .Add(h => h.Photo, "/ada.jpg")
            .Add(h => h.PhotoText, "Ada Lovelace")
            .Add(h => h.Paper, "<span class=\"code\">QR</span>"));

        Assert.Multiple(() =>
        {
            Assert.That(hero.Find(".profile .profile-words h2").TextContent, Is.EqualTo("Ada Lovelace"));
            Assert.That(hero.Find(".profile-words p.summary").TextContent, Is.EqualTo("Writes programs."));
            Assert.That(hero.Find(".profile-words .profile-note").TextContent, Is.EqualTo("London · English"));
            Assert.That(hero.Find(".profile-words .profile-actions a.btn").GetAttribute("href"),
                Is.EqualTo("/contact"));
            Assert.That(hero.Find(".profile > img.profile-photo").GetAttribute("alt"), Is.EqualTo("Ada Lovelace"));
            Assert.That(hero.Find(".profile > .profile-paper .code").TextContent, Is.EqualTo("QR"));
        });
    }

    /// <summary>No photo and no paper row draw neither.</summary>
    [Test]
    public void AHeroWithNoPhoto_DrawsOnlyItsWords()
    {
        using var ctx = new BunitContext();

        var hero = ctx.Render<ProfileHero>(parameters => parameters.Add(h => h.Name, "Ada"));

        Assert.That(hero.FindAll(".profile > *").Select(element => element.ClassName),
            Is.EqualTo(new[] { "profile-words" }));
    }

    /// <summary>The paper row is hidden on a screen, and on paper the buttons give way to it.</summary>
    [TestCase(".profile-paper { display: none; }")]
    [TestCase(".profile-actions { display: none; }")]
    [TestCase("grid-template-columns: repeat(5, minmax(0, 1fr));")]
    public void ThePaperRow_IsForPaper(string rule) =>
        Assert.That(File.ReadAllText(Path.Combine(Web, "Components/Site/Hero/ProfileHero.razor.scss")),
            Does.Contain(rule));
}

using LouisAgent.Site.Content;

namespace LouisAgent.Site.Tests.Content;

/// <summary>
/// F142: Louis's CV is the site's <c>cv.json</c>, embedded, read strictly into the records the page draws. It is a
/// public page, so it holds nothing that is identity-theft material or that weakens a pay talk (POPIA): no ID number,
/// street address, birth date or salary. A later edit that adds one fails here.
/// </summary>
public class CvContentTests
{
    /// <summary>The file as embedded, for the checks that read its words.</summary>
    private static string Text() => Cv.Json;

    /// <summary>It reads, strictly, as Louis's CV.</summary>
    [Test]
    public void TheCv_Reads_AsLouissCv()
    {
        var cv = Cv.Resume;

        Assert.Multiple(() =>
        {
            Assert.That(cv.Basics.Name, Is.EqualTo("Louis Marais"));
            Assert.That(cv.Work, Is.Not.Empty);
            Assert.That(cv.Work[0].Name, Is.EqualTo("Kohde"), "newest first");
            Assert.That(cv.Skills, Is.Not.Empty);
        });
    }

    /// <summary>Its links: LinkedIn, GitHub and WhatsApp, each to Louis's own address.</summary>
    [Test]
    public void TheCv_LinksLinkedInGitHubAndWhatsApp()
    {
        var profiles = Cv.Resume.Basics.Profiles.ToDictionary(profile => profile.Network, profile => profile.Url);

        Assert.Multiple(() =>
        {
            Assert.That(profiles["LinkedIn"], Is.EqualTo("https://www.linkedin.com/in/louismarais86"));
            Assert.That(profiles["GitHub"], Is.EqualTo("https://github.com/bosbevk"));
            Assert.That(profiles["WhatsApp"], Is.EqualTo("https://wa.me/27663912470"));
        });
    }

    /// <summary>No South African ID number: no run of 13 digits anywhere in the file.</summary>
    [Test]
    public void TheCv_HoldsNoIdNumber() =>
        Assert.That(LongestDigitRun(Text()), Is.LessThan(13));

    /// <summary>
    /// No salary, birth date or street address: the words that would name one are nowhere in the file, and the
    /// records have no field for them, so the strict read would refuse one too.
    /// </summary>
    [Test]
    public void TheCv_HoldsNoSalaryBirthDateOrAddress()
    {
        string text = Text().ToLowerInvariant();
        string[] personal = ["salary", "per month", "birth", "\"address\"", "street", "id number"];

        Assert.That(personal.Where(text.Contains), Is.Empty);
    }

    /// <summary>The longest run of digits in the text, spaces between digits counted as part of it.</summary>
    private static int LongestDigitRun(string text)
    {
        int longest = 0, run = 0;
        foreach (char c in text)
        {
            // A digit grows the run, a space keeps it (an ID number written in groups), anything else ends it.
            if (char.IsDigit(c)) run++;
            else if (c != ' ') run = 0;
            longest = Math.Max(longest, run);
        }

        return longest;
    }
}

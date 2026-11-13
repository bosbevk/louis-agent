using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Privacy;

namespace LouisAgent.Site.Tests.Content;

/// <summary>
/// F142: Louis's CV is the site's <c>cv.json</c>, embedded, read strictly into the records the page draws. It is a
/// public page, so it holds nothing that is identity-theft material or that weakens a pay talk (POPIA): no ID number,
/// street address, birth date or salary. A later edit that adds one fails here, as a save from the admin's editor
/// fails on the same check.
/// </summary>
public class CvContentTests
{
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

    /// <summary>It holds no ID number, salary, birth date or street address.</summary>
    [Test]
    public void TheCv_HoldsNothingAPublicPageMustntShow() =>
        Assert.That(ResumePrivacy.Problems(Cv.Json), Is.Empty);
}

using LouisAgent.FilmExport.Films;
using LouisAgent.FilmExport.Rendering;

namespace LouisAgent.FilmExport.Tests;

/// <summary>What the tool asks of the encoder and the browser, and that its list of films matches the site.</summary>
public class FilmExportTests
{
    /// <summary>The encoder's image is pinned, so a re-export months later encodes the same way.</summary>
    [Test]
    public void TheEncoderImage_IsPinned()
    {
        string dockerfile = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, WebPEncoder.Dockerfile));

        Assert.That(dockerfile, Does.Match(@"FROM alpine:[\d.]+@sha256:[0-9a-f]{64}"));
        Assert.That(dockerfile, Does.Match(@"libwebp-tools=[\d.]+-r\d+"));
    }

    /// <summary>The settings reach the encoder, the frames go in read-only, and the file lands as asked.</summary>
    [Test]
    public void TheEncoder_AsksForTheFilmAsAsked()
    {
        var arguments = WebPEncoder.Arguments("C:/frames", "C:/out/short.webp", 3, 24, 75);

        Assert.That(arguments, Does.Contain("C:/frames:/frames:ro").And.Contain("C:/out:/out"));
        Assert.That(arguments, Does.Contain("img2webp").And.Contain("-lossy"));
        Assert.That(string.Join(' ', arguments), Does.Contain("-q 75").And.Contain("-d 42").And.Contain("-loop 0"));
        Assert.That(arguments.TakeLast(5),
            Is.EqualTo(new[] { "f0000.png", "f0001.png", "f0002.png", "-o", "/out/short.webp" }));
    }

    /// <summary>
    /// A keyframe every half second: between keyframes the encoder treats a small change as no change, so a slow fade
    /// on low-contrast text would never be written, and a card would keep the last card's words.
    /// </summary>
    [Test]
    public void TheEncoder_ForcesAKeyframeEveryHalfSecond()
    {
        string arguments = string.Join(' ', WebPEncoder.Arguments("C:/frames", "C:/out/short.webp", 3, 24, 75));

        Assert.That(arguments, Does.Contain("-kmax 12").And.Contain("-kmin 6"));
    }

    /// <summary>
    /// The site it starts is the host it is told: the framework's for its page cards, and louis-agent.dev's for the
    /// films and that site's own card.
    /// </summary>
    [Test]
    public void TheSiteStarted_IsTheHostItIsTold()
    {
        var framework = SiteProcess.Arguments("/repo", "src/site/LouisAgent.SiteFrameworkApi");
        var business = SiteProcess.Arguments("/repo", SiteProcess.BusinessSite);

        Assert.Multiple(() =>
        {
            Assert.That(framework.Last(), Is.EqualTo(Path.Combine("/repo", "src/site/LouisAgent.SiteFrameworkApi")));
            Assert.That(business.Last(), Is.EqualTo(Path.Combine("/repo", "src/site/LouisAgent.SiteApi")));
            Assert.That(framework.Take(3), Is.EqualTo(new[] { "run", "--no-launch-profile", "--project" }));
        });
    }

    /// <summary>Whole seconds of film at a frame rate.</summary>
    [Test]
    public void AFilm_TakesItsLengthTimesTheFrameRate()
    {
        Assert.That(FilmRenderer.FrameCount(FilmSource.Short, 24), Is.EqualTo(864));
        Assert.That(FilmRenderer.FrameCount(FilmSource.Detail, 24), Is.EqualTo(1440));
    }

    /// <summary>The names sort in frame order, from the first frame on.</summary>
    [Test]
    public void AFramesName_SortsInOrder()
    {
        Assert.That(FilmRenderer.FrameName(0), Is.EqualTo("f0000.png"));
        Assert.That(FilmRenderer.FrameName(1439), Is.EqualTo("f1439.png"));
    }
}

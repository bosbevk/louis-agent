using LouisAgent.FilmExport.Films;
using LouisAgent.FilmExport.Rendering;

namespace LouisAgent.FilmExport.Tests;

/// <summary>What the tool asks of ffmpeg and the browser, and that its list of films matches the site.</summary>
public class FilmExportTests
{
    /// <summary>One image by its digest, so a re-export months later uses the same ffmpeg.</summary>
    [Test]
    public void TheEncoder_PinsItsImageByDigest()
    {
        var arguments = FfmpegEncoder.Arguments("C:/frames", "C:/out/short.webp", 24, 75);

        Assert.That(arguments, Has.Some.Matches<string>(a => a.StartsWith("jrottenberg/ffmpeg@sha256:")));
    }

    /// <summary>The settings reach ffmpeg, frames go in read-only, and the file lands where it was asked.</summary>
    [Test]
    public void TheEncoder_AsksForTheFilmAsAsked()
    {
        var arguments = FfmpegEncoder.Arguments("C:/frames", "C:/out/short.webp", 24, 75);

        Assert.That(arguments, Does.Contain("C:/frames:/frames:ro").And.Contain("C:/out:/out"));
        Assert.That(arguments, Does.Contain("libwebp_anim"));
        Assert.That(string.Join(' ', arguments),
            Does.Contain("-framerate 24").And.Contain("-quality 75").And.Contain("-loop 0"));
        Assert.That(arguments[^1], Is.EqualTo("/out/short.webp"));
    }

    /// <summary>Whole seconds of film at a frame rate.</summary>
    [Test]
    public void AFilm_TakesItsLengthTimesTheFrameRate()
    {
        Assert.That(FilmRenderer.FrameCount(FilmSource.Short, 24), Is.EqualTo(864));
        Assert.That(FilmRenderer.FrameCount(FilmSource.Detail, 24), Is.EqualTo(1440));
    }

    /// <summary>The name ffmpeg's pattern reads, from the first frame on.</summary>
    [Test]
    public void AFramesName_FitsFfmpegsPattern()
    {
        Assert.That(FilmRenderer.FrameName(0), Is.EqualTo("f0000.png"));
        Assert.That(FilmRenderer.FrameName(1439), Is.EqualTo("f1439.png"));
    }

    /// <summary>The list names real files, so a moved film can't go unnoticed.</summary>
    [Test]
    public void EveryFilm_NamesFilesThatExist()
    {
        foreach (string file in FilmSource.All.SelectMany(film => film.Files))
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, file)), Is.True, file);
    }

    /// <summary>The tool's length is the stylesheet's loop, or the export would cut a film short or freeze it.</summary>
    [TestCase(FilmName.Short, "short.css", "--loop: 36s")]
    [TestCase(FilmName.Detail, "detail.css", "--loop: 60s")]
    public void AFilmsLength_IsItsStylesheetsLoop(FilmName name, string sheet, string loop)
    {
        var film = FilmSource.All.Single(f => f.Name == name);
        string css = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, film.Files.Single(f => f.EndsWith(sheet))));

        Assert.That(css, Does.Contain(loop));
    }
}

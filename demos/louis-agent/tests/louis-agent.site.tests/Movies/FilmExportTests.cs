using LouisAgent.FilmExport.Films;

namespace LouisAgent.Site.Tests.Movies;

/// <summary>The films in the README are what the site plays: change a film and this fails until it's re-exported.</summary>
public class FilmExportTests
{
    private const string Folder = "demos/louis-agent/docs/films";

    /// <summary>The hash recorded at the export is the film's hash today.</summary>
    [Test]
    public void TheExports_MatchTheFilms()
    {
        string recorded = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, Folder, "films.sha256"));

        foreach (var film in FilmSource.All)
            Assert.That(FilmHash.Read(recorded, film.Name), Is.EqualTo(FilmHash.Of(film, TestPaths.RepoRoot)),
                $"The {film.Name} film changed: run demos/louis-agent/tools/louis-agent.film-export and commit its files.");
    }

    /// <summary>Each film has its animation, and the still for people who asked for less motion.</summary>
    [TestCase(FilmName.Short)]
    [TestCase(FilmName.Detail)]
    public void AFilm_HasItsAnimationAndItsStill(FilmName film)
    {
        string name = film.ToString().ToLowerInvariant();

        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, Folder, $"{name}.webp")), Is.True, $"{name}.webp");
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, Folder, $"{name}.png")), Is.True, $"{name}.png");
    }
}

namespace LouisAgent.FilmExport.Films;

/// <summary>One of the site's films: where it plays, how long it runs, and the files it's made of.</summary>
/// <param name="Name">Its name, which is also its exported file's name.</param>
/// <param name="Route">The site's page that plays it.</param>
/// <param name="Selector">The film's element on that page.</param>
/// <param name="Length">One loop of the film.</param>
/// <param name="Files">What decides how it looks, from the repo's root; a change to any of them means re-exporting.</param>
public sealed record FilmSource(
    FilmName Name,
    string Route,
    string Selector,
    TimeSpan Length,
    IReadOnlyList<string> Files)
{
    private const string Site = "demos/louis-agent/web/louis-agent.site/";

    /// <summary>The 36-second film on the home page.</summary>
    public static FilmSource Short { get; } = new(FilmName.Short, "/", ".movie", TimeSpan.FromSeconds(36),
    [
        Site + "Components/Movies/ShortMovie.razor", Site + "wwwroot/css/movie/stage.css",
        Site + "wwwroot/css/movie/short.css", Site + "wwwroot/css/settings/tokens.css",
    ]);

    /// <summary>The 60-second film on the how-it-works page.</summary>
    public static FilmSource Detail { get; } = new(FilmName.Detail, "/how-it-works", ".movie", TimeSpan.FromSeconds(60),
    [
        Site + "Components/Movies/DetailedMovie.razor", Site + "wwwroot/css/movie/stage.css",
        Site + "wwwroot/css/movie/detail.css", Site + "wwwroot/css/settings/tokens.css",
    ]);

    /// <summary>Every film the site has.</summary>
    public static IReadOnlyList<FilmSource> All { get; } = [Short, Detail];
}

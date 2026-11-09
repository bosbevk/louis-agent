namespace LouisAgent.FilmExport.Films;

/// <summary>
/// One of the site's films, played by the code-review app (F109.1): where it plays, how long it runs, and the files
/// it's made of.
/// </summary>
/// <param name="Name">Its name, which is also its exported file's name.</param>
/// <param name="Route">The app's page that plays it.</param>
/// <param name="Selector">The film's element on that page.</param>
/// <param name="Length">One loop of the film.</param>
/// <param name="Files">What decides how it looks, from the repo's root; a change to one means re-exporting.</param>
public sealed record FilmSource(
    FilmName Name,
    string Route,
    string Selector,
    TimeSpan Length,
    IReadOnlyList<string> Files)
{
    private const string App = "demos/louis-agent/web/louis-agent.code-review/";

    /// <summary>The framework's own site, where the films play since F124.</summary>
    private const string Site = "src/site/louis-agent.site/";

    /// <summary>The site's colours are the framework's, so a change there changes how a film looks.</summary>
    private const string Colours = "src/web/louis-agent.web/Styles/web/settings/_colours.scss";

    /// <summary>The 36-second film on the home page.</summary>
    public static FilmSource Short { get; } = new(FilmName.Short, "/", ".movie", TimeSpan.FromSeconds(36),
    [
        Site + "Components/Movies/ShortMovie.razor", App + "Styles/site/movie/_stage.scss",
        App + "Styles/site/movie/_short.scss", App + "Styles/site/settings/_tokens.scss", Colours,
    ]);

    /// <summary>The 60-second film on the how-it-works page.</summary>
    public static FilmSource Detail { get; } = new(FilmName.Detail, "/how-it-works", ".movie", TimeSpan.FromSeconds(60),
    [
        Site + "Components/Movies/DetailedMovie.razor", App + "Styles/site/movie/_stage.scss",
        App + "Styles/site/movie/_detail.scss", App + "Styles/site/settings/_tokens.scss", Colours,
    ]);

    /// <summary>Every film the site has.</summary>
    public static IReadOnlyList<FilmSource> All { get; } = [Short, Detail];
}

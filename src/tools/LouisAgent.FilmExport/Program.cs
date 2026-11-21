using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Cards;
using LouisAgent.FilmExport.Films;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Messages.Holes;
using LouisAgent.FilmExport.Rendering;
using PuppeteerSharp;

// Renders the site's films to animated WebP for the README, with a still of each for people who asked for less motion,
// and records a hash of what each was made of. Run it after changing a film, and commit what it writes (Change the
// site). With social-card, it draws the site's share card instead, the glass orb photographed from the site (FX174).
//
//   dotnet run --project src/tools/LouisAgent.FilmExport [short] [detail] [--out <folder>]
//   dotnet run --project src/tools/LouisAgent.FilmExport social-card
//
// It needs Docker running, for the encoder. CHROME_PATH names the browser; without it, a Chrome for Testing is
// downloaded once.
const int fps = 24;
const int quality = 75;
const int width = 960;
const string outOption = "--out";
const string socialCard = "social-card";

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string repoRoot = FindRepoRoot();

int outAt = Array.IndexOf(args, outOption);
string output = Path.GetFullPath(outAt >= 0
    ? args[outAt + 1]
    : Path.Combine(repoRoot, "src/docs/films"));
var names = args.Where((_, at) => outAt < 0 || (at != outAt && at != outAt + 1)).ToList();

// The share card: the site's own glass orb, photographed, laid into the card's page, written where the site serves it
// and where the brand's files are kept.
if (names.Contains(socialCard))
{
    using var cardSite = await SiteProcess.StartAsync(repoRoot);
    await using var cardBrowser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true,
        ExecutablePath = await ChromeAsync()
    });
    var card = new CardRenderer(cardBrowser, cardSite.Address);
    byte[] orb = await card.OrbAsync();
    string template = File.ReadAllText(Path.Combine(repoRoot, SocialCardPage.TemplatePath));
    // The site's card is light, its default look; the brand's folder keeps both, to choose between.
    var places = new (string Place, CardLook Look)[]
    {
        ("src/site/LouisAgent.Site/wwwroot/social-card.png", CardLook.Light),
        ("src/docs/brand/social-card.png", CardLook.Light),
        ("src/docs/brand/social-card-light.png", CardLook.Light),
        ("src/docs/brand/social-card-dark.png", CardLook.Dark)
    };
    foreach (var (place, look) in places)
    {
        string path = Path.Combine(repoRoot, place);
        await card.WriteAsync(template, orb, look, path);
        var written = new CardWrittenHoles(path);
        Console.WriteLine(MessageText.Format(FilmExportUserText.CardWritten, written));
    }

    return 0;
}

var films = new List<FilmSource>();
foreach (string name in names)
{
    if (!SettingChoice.TryParse(name, out FilmName? film) || film is null)
    {
        var unknown = new UnknownFilmHoles(name, string.Join(", ", SettingChoice.Names<FilmName>()));
        Console.Error.WriteLine(MessageText.Format(FilmExportUserText.UnknownFilm, unknown));
        return 2;
    }

    films.Add(FilmSource.All.Single(source => source.Name == film));
}

if (films.Count == 0) films.AddRange(FilmSource.All);

Directory.CreateDirectory(output);
string hashFile = Path.Combine(output, "films.sha256");

using var site = await SiteProcess.StartAsync(repoRoot);
await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
{
    Headless = true,
    ExecutablePath = await ChromeAsync()
});
var renderer = new FilmRenderer(browser, site.Address);

foreach (var film in films)
{
    string name = film.Name.Wire();
    string frames = Directory.CreateTempSubdirectory("film-export-").FullName;
    try
    {
        var rendering = new RenderingHoles(name, FilmRenderer.FrameCount(film, fps));
        Console.WriteLine(MessageText.Format(FilmExportUserText.Rendering, rendering));
        await renderer.RenderAsync(film, frames, fps, width);

        var encoding = new EncodingHoles(name);
        Console.WriteLine(MessageText.Format(FilmExportUserText.Encoding, encoding));
        string webp = Path.Combine(output, $"{name}.webp");
        WebPEncoder.ToWebP(repoRoot, frames, webp, FilmRenderer.FrameCount(film, fps), fps, quality);

        // The first frame is the wide view with every part: the still for reduced motion.
        File.Copy(Path.Combine(frames, FilmRenderer.FrameName(0)), Path.Combine(output, $"{name}.png"),
            overwrite: true);

        string? recorded = File.Exists(hashFile) ? File.ReadAllText(hashFile) : null;
        File.WriteAllText(hashFile, FilmHash.Record(recorded, film, FilmHash.Of(film, repoRoot)));

        var exported = new ExportedHoles(name, webp, (new FileInfo(webp).Length / 1024d / 1024d).ToString("F2"));
        Console.WriteLine(MessageText.Format(FilmExportUserText.Exported, exported));
    }
    finally
    {
        Directory.Delete(frames, recursive: true);
    }
}

return 0;

// The repo's root, from wherever this was started inside it.
static string FindRepoRoot()
{
    string start = Directory.GetCurrentDirectory();
    for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        if (File.Exists(Path.Combine(folder.FullName, "louis-agent-solution.sln")))
            return folder.FullName;

    var notFound = new RepoNotFoundHoles(start);
    throw new InvalidOperationException(MessageText.Format(FilmExportUserText.RepoNotFound, notFound));
}

// The browser named by CHROME_PATH, or a Chrome for Testing kept out of the repo in the temp folder.
static async Task<string> ChromeAsync()
{
    if (Environment.GetEnvironmentVariable("CHROME_PATH") is { Length: > 0 } named) return named;

    var fetcher = new BrowserFetcher(new BrowserFetcherOptions
    {
        Path = Path.Combine(Path.GetTempPath(), "louis-agent-film-export-chrome"),
    });
    return (await fetcher.DownloadAsync()).GetExecutablePath();
}

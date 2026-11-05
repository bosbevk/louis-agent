using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Films;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Rendering;
using PuppeteerSharp;

// Renders the site's films to animated WebP for the README, with a still of each for people who asked for less motion,
// and records a hash of what each was made of. Run it after changing a film, and commit what it writes (Change the
// site).
//
//   dotnet run --project demos/louis-agent/tools/louis-agent.film-export [short] [detail] [--out <folder>]
//
// It needs Docker running, for the encoder. CHROME_PATH names the browser; without it, a Chrome for Testing is
// downloaded once.
const int fps = 24;
const int quality = 75;
const int width = 960;
const string outOption = "--out";

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string repoRoot = FindRepoRoot();

int outAt = Array.IndexOf(args, outOption);
string output = Path.GetFullPath(outAt >= 0
    ? args[outAt + 1]
    : Path.Combine(repoRoot, "demos/louis-agent/docs/films"));
var names = args.Where((_, at) => outAt < 0 || (at != outAt && at != outAt + 1)).ToList();

var films = new List<FilmSource>();
foreach (string name in names)
{
    if (!SettingChoice.TryParse(name, out FilmName? film) || film is null)
    {
        Console.Error.WriteLine(MessageText.Format(FilmExportUserText.UnknownFilm,
            new
            {
                Name = name,
                Supported = string.Join(", ", SettingChoice.Names<FilmName>())
            }));
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
        Console.WriteLine(MessageText.Format(FilmExportUserText.Rendering,
            new
            {
                Film = name,
                Frames = FilmRenderer.FrameCount(film, fps)
            }));
        await renderer.RenderAsync(film, frames, fps, width);

        Console.WriteLine(MessageText.Format(FilmExportUserText.Encoding, new { Film = name }));
        string webp = Path.Combine(output, $"{name}.webp");
        WebPEncoder.ToWebP(repoRoot, frames, webp, FilmRenderer.FrameCount(film, fps), fps, quality);

        // The first frame is the wide view with every part: the still for reduced motion.
        File.Copy(Path.Combine(frames, FilmRenderer.FrameName(0)), Path.Combine(output, $"{name}.png"),
            overwrite: true);

        string? recorded = File.Exists(hashFile) ? File.ReadAllText(hashFile) : null;
        File.WriteAllText(hashFile, FilmHash.Record(recorded, film, FilmHash.Of(film, repoRoot)));

        Console.WriteLine(MessageText.Format(FilmExportUserText.Exported,
            new
            {
                Film = name,
                Path = webp,
                Megabytes = (new FileInfo(webp).Length / 1024d / 1024d).ToString("F2")
            }));
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

    throw new InvalidOperationException(MessageText.Format(FilmExportUserText.RepoNotFound, new { Folder = start }));
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

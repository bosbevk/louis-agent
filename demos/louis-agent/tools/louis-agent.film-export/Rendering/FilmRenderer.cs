using LouisAgent.FilmExport.Films;
using PuppeteerSharp;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>Steps a film's animations and takes a picture of each frame.</summary>
/// <remarks>
/// It sets each animation's time instead of filming the page as it plays, so every frame lands exactly where it should
/// however slow the machine is. The CSS stays the film's only source (ADR 0066).
/// </remarks>
public sealed class FilmRenderer(IBrowser browser, Uri site)
{
    private const string FontsReady = "() => document.fonts.ready.then(() => true)";

    private const string SizeTheFilm = """
        (selector, width) => {
            const film = document.querySelector(selector);
            film.style.width = `${width}px`;
            film.style.maxWidth = `${width}px`;
        }
        """;

    private const string Seek = """
        (selector, ms) => document.querySelector(selector).getAnimations({ subtree: true }).forEach(animation => {
            animation.pause();
            animation.currentTime = ms;
        })
        """;

    /// <summary>How many frames one loop of a film takes.</summary>
    public static int FrameCount(FilmSource film, int fps) => (int)(film.Length.TotalSeconds * fps);

    /// <summary>A frame's file name, which sorts in frame order.</summary>
    public static string FrameName(int frame) => $"f{frame:D4}.png";

    /// <summary>Writes one loop of the film into a folder, a picture per frame.</summary>
    public async Task RenderAsync(FilmSource film, string framesFolder, int fps, int width)
    {
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = width + 200, Height = width, DeviceScaleFactor = 1 });
        await page.GoToAsync(new Uri(site, film.Route).AbsoluteUri, WaitUntilNavigation.Networkidle0);

        // The right fonts before the first picture, or the first frames would show the fallback.
        await page.EvaluateFunctionAsync<bool>(FontsReady);
        await page.EvaluateFunctionAsync(SizeTheFilm, film.Selector, width);

        var element = await page.QuerySelectorAsync(film.Selector);
        int frames = FrameCount(film, fps);
        for (int frame = 0; frame < frames; frame++)
        {
            await page.EvaluateFunctionAsync(Seek, film.Selector, frame * 1000.0 / fps);
            await element.ScreenshotAsync(Path.Combine(framesFolder, FrameName(frame)));
        }
    }
}

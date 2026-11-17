using LouisAgent.FilmExport.Cards;
using PuppeteerSharp;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>
/// Draws the share card (FX174): photographs the glass orb the site's brand section shows, alone on nothing, then lays
/// it into the card's page and photographs that.
/// </summary>
/// <remarks>
/// The orb is taken from the running site, never redrawn here, so the card's glass is exactly the site's, whatever the
/// theme makes of it next. The card's own page is a template file, filled by <see cref="SocialCardPage"/>.
/// </remarks>
internal sealed class CardRenderer(IBrowser browser, Uri site)
{
    /// <summary>The landing's brand section, where the orb stands large.</summary>
    private const string BrandSection = "/home/brand";

    /// <summary>The orb's frame in the brand section, photographed with the margin its light needs.</summary>
    private const string Orb = ".mark-large";

    /// <summary>
    /// How sharp the orb is photographed: eight pixels for each of its points, so the card only ever draws it smaller,
    /// never stretched (Louis: "super crisp").
    /// </summary>
    private const int Sharpness = 8;

    /// <summary>
    /// How many pixels the card takes for each of its points: twice, so it stays sharp on sharp screens and drawn
    /// large (Louis: "highest quality you have?").
    /// </summary>
    private const int CardSharpness = 2;

    /// <summary>
    /// Leaves the orb alone on nothing, in the site's default look, glass and light (Louis: "the orb light is
    /// default"), at rest: everything else unseen, the page's own ground and lights taken away, no motion. It keeps the
    /// size the site draws it at, since parts of its drawing are measured in pixels and show a seam at any other.
    /// </summary>
    private const string OrbAlone =
        "document.documentElement.dataset.theme = 'glass'; document.documentElement.dataset.style = 'light';" +
        "const style = document.createElement('style');" +
        "style.textContent = '*, *::before, *::after { animation: none !important; transition: none !important; }" +
        " html, body { background: none !important; } body::before, body::after { display: none !important; }" +
        " body * { visibility: hidden !important; } .mark-large .orb, .mark-large .orb * { visibility: visible !important; }" +
        " .mark-large { padding: 18px !important; }';" +
        "document.head.append(style);";

    /// <summary>The site's glass orb, alone, as a PNG with a clear ground.</summary>
    public async Task<byte[]> OrbAsync()
    {
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 900, DeviceScaleFactor = Sharpness });
        await page.GoToAsync(new Uri(site, BrandSection).AbsoluteUri, WaitUntilNavigation.Networkidle0);
        var orb = await page.WaitForSelectorAsync(Orb);
        await page.EvaluateExpressionAsync(OrbAlone);
        await page.EvaluateExpressionAsync("document.fonts.ready");

        // The element itself, so the photograph finds it wherever the page has scrolled; its padding holds the light.
        return await orb.ScreenshotDataAsync(new ElementScreenshotOptions { OmitBackground = true });
    }

    /// <summary>Lays the orb into the card's page, in this look, and photographs the card into this file.</summary>
    public async Task WriteAsync(string template, byte[] orb, CardLook look, string path)
    {
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = SocialCardPage.Width,
            Height = SocialCardPage.Height,
            DeviceScaleFactor = CardSharpness
        });
        await page.SetContentAsync(SocialCardPage.Fill(template, orb, look));
        // The type comes from Google's fonts: wait until nothing more is loading, then until the fonts are in.
        await page.WaitForNetworkIdleAsync();
        await page.EvaluateExpressionAsync("document.fonts.ready");
        await page.ScreenshotAsync(path, new ScreenshotOptions { Type = ScreenshotType.Png });
    }
}

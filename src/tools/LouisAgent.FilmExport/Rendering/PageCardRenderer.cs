using LouisAgent.FilmExport.Cards;
using LouisAgent.FilmExport.Cards.Pages;
using PuppeteerSharp;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>
/// Draws the framework's page cards (F169.6) from its running site: photographs the site's name alone, reads a page's
/// words as a crawler reads them, and lays a card out, measures it and photographs it.
/// </summary>
/// <remarks>
/// The name is taken from the running site, never drawn here, as <see cref="CardRenderer"/> takes the glass ball, so
/// the card's name is exactly the site's. A page's words are read with no script run, from the HTML the host answers,
/// so the card says what a crawler reads. Whether a card is right is decided by <see cref="CardFit"/>, apart from the
/// browser; this class only drives it.
/// </remarks>
internal sealed class PageCardRenderer(IBrowser browser, Uri site)
{
    /// <summary>The name's frame in the site's header.</summary>
    private const string Name = ".wordmark";

    /// <summary>How sharp the name is photographed: four pixels a point, so a card only ever shrinks it.</summary>
    private const int NameSharpness = 4;

    /// <summary>How many pixels a card takes for each of its points: twice, as louis-agent.dev's card.</summary>
    private const int CardSharpness = 2;

    /// <summary>The page the name is photographed on, the site's root.</summary>
    private const string Root = "/";

    /// <summary>
    /// Leaves the name alone on nothing, in flat's light look, the look the site opens in, at rest and at the size
    /// asked for: everything else unseen, the page's ground taken away. The header's line is tight, so the name gets
    /// room below it, or the g's tail is cut off at a card's size.
    /// </summary>
    private const string NameAlone =
        "size => { document.documentElement.dataset.theme = 'flat'; document.documentElement.dataset.style = 'light';" +
        " let style = document.getElementById('alone');" +
        " if (!style) { style = document.createElement('style'); style.id = 'alone'; document.head.append(style); }" +
        " style.textContent = '*, *::before, *::after { animation: none !important; transition: none !important; }" +
        " html, body { background: none !important; } body::before, body::after { display: none !important; }" +
        " body * { visibility: hidden !important; } .wordmark, .wordmark * { visibility: visible !important; }" +
        " .wordmark { font-size: ' + size + 'px !important; overflow: visible !important;" +
        " padding-bottom: .22em !important; }'; }";

    /// <summary>A page's title, the site's name and the first heading, read from the HTML with no script run.</summary>
    private const string ReadWords =
        "() => [document.title, document.querySelector('meta[property=\"og:site_name\"]')?.content ?? ''," +
        " document.querySelector('h1')?.textContent.trim() ?? '']";

    /// <summary>
    /// Whether the card's type loaded: Geist and Geist Mono are among the faces the page loaded, and the face each of
    /// its words asks for is in. A browser loads only the faces a page uses, so each word's own face is what is asked.
    /// </summary>
    private const string TypeLoaded =
        "() => { const loaded = [...document.fonts].filter(face => face.status === 'loaded')" +
        ".map(face => face.family.replaceAll('\"', ''));" +
        " return loaded.includes('Geist') && loaded.includes('Geist Mono')" +
        " && [...document.querySelectorAll('p.at')]" +
        ".every(word => document.fonts.check(getComputedStyle(word).font)); }";

    /// <summary>Where the card's rightmost word ends, in points: each word's text, and the name's picture.</summary>
    private const string WordsEnd =
        "() => Math.max(0, ...[...document.querySelectorAll('.at')].map(element => {" +
        " if (element.tagName === 'IMG') return element.getBoundingClientRect().right;" +
        " const range = document.createRange(); range.selectNodeContents(element);" +
        " return range.getBoundingClientRect().right; }))";

    /// <summary>The site's name, alone on nothing, photographed at this size in points.</summary>
    public async Task<CardName> NameAsync(int size)
    {
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1280,
            Height = 900,
            DeviceScaleFactor = NameSharpness
        });
        await page.GoToAsync(new Uri(site, Root).AbsoluteUri, WaitUntilNavigation.Networkidle0);
        var name = await page.WaitForSelectorAsync(Name);
        await page.EvaluateFunctionAsync(NameAlone, size);
        await page.EvaluateExpressionAsync("document.fonts.ready");

        var box = await name.BoundingBoxAsync();
        byte[] picture = await name.ScreenshotDataAsync(new ElementScreenshotOptions { OmitBackground = true });
        return new CardName(picture, Math.Round((double)box.Width, 2), Math.Round((double)box.Height, 2));
    }

    /// <summary>A page's words, as the host answers it to a crawler that runs no script.</summary>
    public async Task<PageCardWords> WordsAsync(SitePage sitePage)
    {
        await using var page = await browser.NewPageAsync();
        await page.SetJavaScriptEnabledAsync(false);
        await page.GoToAsync(new Uri(site, sitePage.Route).AbsoluteUri, WaitUntilNavigation.DOMContentLoaded);

        // The heading is in the hidden prerendered part, so its textContent is read, never its innerText.
        string[] words = await page.EvaluateFunctionAsync<string[]>(ReadWords);
        return PageCardWords.Of(words[0], words[1], words[2], sitePage.Address);
    }

    /// <summary>
    /// Photographs a card's page into this file, once its type has loaded and its words end inside the middle; else
    /// writes nothing and throws with the reason. It gives back where the words end, in points.
    /// </summary>
    public async Task<double> WriteAsync(string html, string route, string path)
    {
        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = SocialCardPage.Width,
            Height = SocialCardPage.Height,
            DeviceScaleFactor = CardSharpness
        });
        await page.SetContentAsync(html);
        // The type comes from Google's fonts: wait until nothing more is loading, then until the fonts are in.
        await page.WaitForNetworkIdleAsync();
        await page.EvaluateExpressionAsync("document.fonts.ready");

        bool typeLoaded = await page.EvaluateFunctionAsync<bool>(TypeLoaded);
        double endsAt = await page.EvaluateFunctionAsync<double>(WordsEnd);
        if (CardFit.Check(route, endsAt, typeLoaded) is { } refusal) throw new InvalidOperationException(refusal);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await page.ScreenshotAsync(path, new ScreenshotOptions { Type = ScreenshotType.Png });
        return endsAt;
    }
}

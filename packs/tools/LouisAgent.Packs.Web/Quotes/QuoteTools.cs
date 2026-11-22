using LouisAgent.Composition.Messages.Holes.Errors;
using LouisAgent.Composition.Web.Messages.Holes.Quotes;
using LouisAgent.Composition.Web.Messages.Holes.Fetch;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Reading;
using LouisAgent.Composition.Web.Messages;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using System.Net;

namespace LouisAgent.Packs.Web.Quotes;

/// <summary>
/// The web toolset's check of a price's provenance (F30, FX16; S: one of its three classes): the exact words a value
/// came from must be on the page, by <see cref="QuoteMatch"/>'s rules, with no model reading the page.
/// </summary>
public sealed class QuoteTools(IPageFetcher fetcher, IPageRenderer? renderer = null)
{
    private readonly IPageFetcher _fetcher = fetcher;

    private readonly IPageRenderer _renderer =
        renderer ?? new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));

    [Description("""
                 Checks that the exact words a price (or any fact) came from are on the page itself: fetches the page
                 with FetchUrl's address rules and looks for the quote with spacing and case ignored. With product, the
                 quote must stand right after the product's name, no other price between; a quote that is only a price
                 needs it. Answers 'Found', 'Not found' or 'Not found with the product' (treat the value as
                 unconfirmed), 'Page empty' (a JavaScript page that couldn't be read) or an error with its reason. No
                 model reads the page, so a check costs nothing but the fetch. Use it on every quote a lookup agent
                 returns before you use its value.
                 """)]
    public async Task<string> VerifyQuote(
        [Description("Absolute http(s) URL of the page the value came from")]
        string url,
        [Description("The exact words the value came from, as the lookup agent (or you) copied them")]
        string quote,
        [Description("""
                     The product's name as the page shows it. Needed when the quote is only a price (R22.99): a price
                     alone matches any product at that price on the page.
                     """)]
        string? product = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate((url ?? "").Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return MessageText.Format(WebToolMessage.FetchUrlInvalid);
        string wanted = QuoteMatch.Normalise((quote ?? "").Trim().Trim('"', '“', '”', '\''));
        if (wanted.Length == 0) return MessageText.Format(WebToolMessage.QuoteEmpty);
        string? named = string.IsNullOrWhiteSpace(product)
            ? null
            : QuoteMatch.Normalise(product.Trim().Trim('"', '“', '”', '\''));
        // FX16: R22.99 is on a page of eggs, milk and butter for the eggs and the milk; alone it says nothing of whose
        // it is.
        if (named is null && QuoteMatch.IsOnlyAPrice(wanted))
        {
            var onlyAPrice = new QuoteOnlyAPriceHoles(quote!.Trim());
            return MessageText.Format(WebToolMessage.QuoteOnlyAPrice, onlyAPrice);
        }

        bool Matches(string text) => named is null
            ? QuoteMatch.Normalise(text).Contains(wanted, StringComparison.Ordinal)
            : QuoteMatch.StandsAfter(QuoteMatch.Normalise(text), named, wanted);

        bool OnPage(string text) => QuoteMatch.Normalise(text).Contains(wanted, StringComparison.Ordinal);

        try
        {
            var (response, finalUri) = await _fetcher.GetFollowingRedirectsAsync(uri);
            using var _ = response;
            if (!response.IsSuccessStatusCode)
            {
                var httpError = new FetchHttpErrorHoles(finalUri.ToString(), (int)response.StatusCode,
                    response.ReasonPhrase);
                return MessageText.Format(WebToolMessage.FetchHttpError, httpError);
            }

            // What the quote's check answers, about this page.
            var found = new QuoteFoundHoles(finalUri.ToString());
            var notFound = new QuoteNotFoundHoles(finalUri.ToString());
            var notWithProduct = new QuoteNotWithProductHoles(finalUri.ToString(), product?.Trim() ?? "",
                QuoteMatch.ProductWindowChars);
            var empty = new QuotePageEmptyHoles(finalUri.ToString());

            string body = await response.Content.ReadAsStringAsync();
            // The whole body with every tag a space (a price per kilo can sit outside <main>); the readable text only
            // tells a JavaScript shell from a page.
            var (_, text) = HtmlText.ToText(body, finalUri);
            string all = WebUtility.HtmlDecode(HtmlText.TagsAsSpaces(body));
            if (Matches(all)) return MessageText.Format(WebToolMessage.QuoteFound, found);
            if (named is not null && OnPage(all))
                return MessageText.Format(WebToolMessage.QuoteNotWithProduct, notWithProduct);
            if (!QuoteMatch.LooksLikeShell(text, all, body.Length))
                return MessageText.Format(WebToolMessage.QuoteNotFound, notFound);

            // Nothing readable: a page drawn with JavaScript. The renderer reads it as a browser shows it (slower).
            try
            {
                var page = await _renderer.RenderAsync(finalUri, cancellationToken);
                if (QuoteMatch.Normalise(page.Text).Length == 0)
                    return MessageText.Format(WebToolMessage.QuotePageEmpty, empty);
                if (Matches(page.Text)) return MessageText.Format(WebToolMessage.QuoteFound, found);
                return named is not null && OnPage(page.Text)
                    ? MessageText.Format(WebToolMessage.QuoteNotWithProduct, notWithProduct)
                    : MessageText.Format(WebToolMessage.QuoteNotFound, notFound);
            }
            catch (PageRenderException)
            {
                return MessageText.Format(WebToolMessage.QuotePageEmpty, empty);
            }
        }
        catch (BlockedAddressException ex)
        {
            var blocked = new ErrorWithReasonHoles(ex.Message);
            return MessageText.Format(ToolMessage.ErrorWithReason, blocked);
        }
        catch (HttpRequestException ex) when (ex.InnerException is BlockedAddressException inner)
        {
            var blocked = new ErrorWithReasonHoles(inner.Message);
            return MessageText.Format(ToolMessage.ErrorWithReason, blocked);
        }
        catch (Exception ex) when
            (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            var failed = new FetchFailedHoles(uri.ToString(), ex.Message);
            return MessageText.Format(WebToolMessage.FetchFailed, failed);
        }
    }
}

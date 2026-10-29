
using System.ComponentModel;
using System.Net;
using LouisAgent.Core.Guardrails.Network;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tools.Web.Quotes;
/// <summary>
/// The web toolset's check of a price's provenance (F30, FX16; S: one of its three classes): the exact words a value
/// came from must be on the page, by <see cref="QuoteMatch"/>'s rules, with no model reading the page.
/// </summary>
public sealed class QuoteTools(PageFetcher? fetcher = null, IPageRenderer? renderer = null)
{
    private readonly PageFetcher _fetcher = fetcher ?? new PageFetcher();

    private readonly IPageRenderer _renderer =
        renderer ?? new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));

    [Description(
        "Checks that the exact words a price (or any fact) came from are on the page itself: fetches the page " +
        "with FetchUrl's address rules and looks for the quote with spacing and case ignored. With product, the quote " +
        "must stand right after the product's name, no other price between; a quote that is only a price needs it. " +
        "Answers 'Found', 'Not found' or 'Not found with the product' (treat the value as unconfirmed), 'Page empty' " +
        "(a JavaScript page that couldn't be read) or an error with its reason. No model reads the page, so a check " +
        "costs nothing but the fetch. Use it on every quote a minion returns before you use its value.")]
    public async Task<string> VerifyQuote(
        [Description("Absolute http(s) URL of the page the value came from")]
        string url,
        [Description("The exact words the value came from, as the minion (or you) copied them")]
        string quote,
        [Description(
            "The product's name as the page shows it. Needed when the quote is only a price (R22.99): a price " +
            "alone matches any product at that price on the page.")]
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
        // FX16: R22.99 is on a page of eggs, milk and butter for the eggs and the milk; alone it says nothing of whose it is.
        if (named is null && QuoteMatch.IsOnlyAPrice(wanted))
            return MessageText.Format(WebToolMessage.QuoteOnlyAPrice, new { Quote = quote!.Trim() });

        bool Matches(string text) => named is null
            ? QuoteMatch.Normalise(text).Contains(wanted, StringComparison.Ordinal)
            : QuoteMatch.StandsAfter(QuoteMatch.Normalise(text), named, wanted);

        bool OnPage(string text) => QuoteMatch.Normalise(text).Contains(wanted, StringComparison.Ordinal);

        try
        {
            var (response, finalUri) = await _fetcher.GetFollowingRedirectsAsync(uri);
            using var _ = response;
            if (!response.IsSuccessStatusCode)
                return MessageText.Format(WebToolMessage.FetchHttpError,
                    new { Url = finalUri, Status = (int)response.StatusCode, Reason = response.ReasonPhrase });

            string body = await response.Content.ReadAsStringAsync();
            // The whole body with every tag a space (a price per kilo can sit outside <main>); the readable text only
            // tells a JavaScript shell from a page.
            var (_, text) = HtmlText.ToText(body, finalUri);
            string all = WebUtility.HtmlDecode(HtmlText.TagsAsSpaces(body));
            if (Matches(all))
                return MessageText.Format(WebToolMessage.QuoteFound, new { Url = finalUri });
            if (named is not null && OnPage(all))
                return MessageText.Format(WebToolMessage.QuoteNotWithProduct,
                    new { Url = finalUri, Product = product!.Trim(), Window = QuoteMatch.ProductWindowChars });
            if (!QuoteMatch.LooksLikeShell(text, all, body.Length))
                return MessageText.Format(WebToolMessage.QuoteNotFound, new { Url = finalUri });

            // Nothing readable: a page drawn with JavaScript. The renderer reads it as a browser shows it (slower).
            try
            {
                var page = await _renderer.RenderAsync(finalUri, cancellationToken);
                if (QuoteMatch.Normalise(page.Text).Length == 0)
                    return MessageText.Format(WebToolMessage.QuotePageEmpty, new { Url = finalUri });
                if (Matches(page.Text)) return MessageText.Format(WebToolMessage.QuoteFound, new { Url = finalUri });
                return named is not null && OnPage(page.Text)
                    ? MessageText.Format(WebToolMessage.QuoteNotWithProduct,
                        new { Url = finalUri, Product = product!.Trim(), Window = QuoteMatch.ProductWindowChars })
                    : MessageText.Format(WebToolMessage.QuoteNotFound, new { Url = finalUri });
            }
            catch (PageRenderException)
            {
                return MessageText.Format(WebToolMessage.QuotePageEmpty, new { Url = finalUri });
            }
        }
        catch (BlockedAddressException ex)
        {
            return MessageText.Format(ToolMessage.ErrorWithReason, new { Reason = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.InnerException is BlockedAddressException blocked)
        {
            return MessageText.Format(ToolMessage.ErrorWithReason, new { Reason = blocked.Message });
        }
        catch (Exception ex) when
            (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return MessageText.Format(WebToolMessage.FetchFailed, new { Url = uri, Reason = ex.Message });
        }
    }
}

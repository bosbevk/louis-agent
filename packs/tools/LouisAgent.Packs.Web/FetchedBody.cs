using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Messages.Holes.Files;
using LouisAgent.Composition.Web.Messages;
using LouisAgent.Composition.Web.Messages.Holes.Fetch;
using LouisAgent.Composition.Web.Reading;

namespace LouisAgent.Packs.Web;

/// <summary>
/// What FetchUrl hands the model for a page that answered: a PDF as its text with page markers, a web page as its
/// readable text and title, plain text, JSON or XML as it came, and anything else refused by its kind.
/// </summary>
/// <remarks>
/// Fowler's <i>Extract Class</i>, as <see cref="PageText"/> was taken out of <see cref="WebTools"/> (CL15): the
/// reading of a body by its kind is one job apart from fetching it, and keeps <see cref="WebTools"/> within its size
/// (CL69).
/// </remarks>
internal static class FetchedBody
{
    /// <summary>The body of <paramref name="response"/> as the model reads it, from <paramref
    /// name="startIndex"/>.</summary>
    internal static async Task<string> ReadAsync(HttpResponseMessage response, Uri finalUri, int startIndex,
        int maxChars, CancellationToken cancellationToken)
    {
        string mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "text/html";

        string? title = null;
        string text;
        if (mediaType == "application/pdf" || (mediaType == "application/octet-stream" &&
                                               finalUri.AbsolutePath.EndsWith(".pdf",
                                                   StringComparison.OrdinalIgnoreCase)))
        {
            // A PDF on the web reads as text with page markers, like ReadDocument (F22).
            await using var pdf = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                text = PdfText.Read(pdf);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // PdfPig throws more than one kind for a damaged file; every one is the same answer to the model.
                var unreadable = new DocumentUnreadableHoles(finalUri.ToString(), ex.Message);
                return MessageText.Format(ToolMessage.DocumentUnreadable, unreadable);
            }

            return PageText.Frame(finalUri.ToString(), null, text, startIndex, maxChars, nameof(WebTools.FetchUrl));
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (mediaType is "text/html" or "application/xhtml+xml")
            (title, text) = HtmlText.ToText(body, finalUri);
        else if (mediaType.StartsWith("text/") || mediaType.Contains("json") || mediaType.Contains("xml"))
            text = body.Trim();
        else
        {
            var notText = new FetchNotTextHoles(finalUri.ToString(), mediaType);
            return MessageText.Format(WebToolMessage.FetchNotText, notText);
        }

        return PageText.Frame(finalUri.ToString(), title, text, startIndex, maxChars, nameof(WebTools.FetchUrl));
    }
}

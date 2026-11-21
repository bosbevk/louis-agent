using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Web.Chats.Calls;

/// <summary>
/// The choices an Offer call put to the visitor (F127.4.1), read from the call's input; any other call, a failed one
/// (the pack refused it, and the model offers again), or one that isn't whole yet, offers nothing.
/// </summary>
/// <remarks>
/// The same shape as <see cref="ShowPageCall"/> and <see cref="CardCall"/>: the web names the tool, never the pack. The
/// pack checks the choices and says a refusal as an error, which the stream marks failed, so the web keeps no copy of
/// the rules: a call that wasn't failed is one the pack offered.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md">ADR 0105: the guide acts on the page through a tool the widget hears</seealso>
public static class OfferCall
{
    /// <summary>The site pack's tool that offers the visitor choices.</summary>
    public const string Tool = "Offer";

    /// <summary>A call's names in any case, as a model may send them.</summary>
    // A box's kind comes by its name, as a form's fields do (FX147).
    private static readonly JsonSerializerOptions CallJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    /// <summary>What an Offer call offers; none for any other call.</summary>
    public static Offered? Of(Block call)
    {
        if (call.ToolName != Tool || call.ToolFailed || call.ToolInput is null) return null;
        // A model's input that isn't whole offers nothing, rather than failing the visitor's page.
        try
        {
            var offered = JsonSerializer.Deserialize<Offered>(call.ToolInput, CallJson);
            return offered is { Question: not null, Choices.Count: > 0 } ? offered : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

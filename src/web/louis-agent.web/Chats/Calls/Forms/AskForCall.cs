using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>
/// The form an AskFor call asked the visitor with (F127.4.3), read from the call's input; any other call, a failed one
/// (the pack refused it, and the model asks again), or one that isn't whole yet, asks nothing.
/// </summary>
/// <remarks>
/// The same shape as <see cref="OfferCall"/>: the web names the tool, never the pack. The pack checks the form and says
/// a refusal as an error, which the stream marks failed, so the web keeps no copy of the rules: a call that wasn't
/// failed is one the pack asked.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md">ADR 0105: the guide acts on the page through a tool the widget hears</seealso>
public static class AskForCall
{
    /// <summary>The site pack's tool that asks the visitor with a form.</summary>
    public const string Tool = "AskFor";

    /// <summary>A call's names in any case, as a model may send them, and its kinds by name only.</summary>
    private static readonly JsonSerializerOptions CallJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    /// <summary>What an AskFor call asks; none for any other call.</summary>
    public static Asked? Of(Block call)
    {
        if (call.ToolName != Tool || call.ToolFailed || call.ToolInput is null) return null;
        // A model's input that isn't whole asks nothing, rather than failing the visitor's page.
        try
        {
            var asked = JsonSerializer.Deserialize<Asked>(call.ToolInput, CallJson);
            return asked is { Question: not null, Fields.Count: > 0 } ? asked : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

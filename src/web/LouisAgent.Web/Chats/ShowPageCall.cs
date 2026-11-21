using System.Text.Json;

namespace LouisAgent.Web.Chats;

/// <summary>
/// Where a ShowPage call goes (F117, shared since F127.1): the address the guide named, read from the call's input, so
/// the guide's widget and a showroom's mock website both move to it; any other call goes nowhere.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md">ADR 0105: the guide acts on the page through a tool the widget hears</seealso>
public static class ShowPageCall
{
    /// <summary>
    /// The site pack's tool that names a page to show. The web names the tool and never the pack, which it can't
    /// reference (ADR 0027): a pack is data to the web, and this is the one call it acts on.
    /// </summary>
    public const string Tool = "ShowPage";

    /// <summary>The address a ShowPage call names; none for any other call, or one with no address.</summary>
    public static string? PathOf(Block call)
    {
        if (call.ToolName != Tool || call.ToolInput is null) return null;
        // A model's input that isn't JSON names no address; the call goes nowhere rather than failing the page.
        try
        {
            using var input = JsonDocument.Parse(call.ToolInput);
            if (!input.RootElement.TryGetProperty("path", out var path)) return null;
            return path.ValueKind == JsonValueKind.String ? path.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

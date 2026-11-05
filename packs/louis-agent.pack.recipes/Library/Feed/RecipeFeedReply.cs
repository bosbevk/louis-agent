using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// What the open recipes feed says: a notice, and every recipe with its site's credit and link. The pack writes this
/// and reads it back, so the feed's first reader is ourselves.
/// </summary>
/// <remarks>A record because it is a plain value (ADR 0040), and JSON goes through records (ADR 0041).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record RecipeFeedReply(
    [property: JsonPropertyName("notice")] string Notice,
    [property: JsonPropertyName("items")] IReadOnlyList<RecipeFeedRow> Recipes)
{
    /// <summary>The reply as its JSON.</summary>
    public string Write() => JsonSerializer.Serialize(this);

    /// <summary>A reply from its JSON.</summary>
    public static RecipeFeedReply Read(string json) => JsonSerializer.Deserialize<RecipeFeedReply>(json)!;
}

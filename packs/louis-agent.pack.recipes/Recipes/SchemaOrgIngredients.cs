using System.Text.Json;

namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// Reads a page's ingredient lines, and tells the lines that are only a heading ("***Filling",
/// "&lt;h3&gt;Sauce&lt;/h3&gt;") from the ones you put in the bowl. A heading is not an ingredient: it names the group
/// of the lines under it.
/// </summary>
/// <remarks>
/// Adapter, kept apart from <see cref="SchemaOrgRecipe"/> for the same reason as <see cref="SchemaOrgValues"/>: the
/// mapper says which field becomes what, and this is the one place that knows how sites write a heading inside a list
/// of lines, the way <c>HowToSection</c> gives the steps theirs. A heading is a whole line in <c>&lt;h1&gt;</c> to
/// <c>&lt;h6&gt;</c>, or a line that starts with two or more stars or hashes. A bullet of one star or dash, and a line
/// ending in a colon, are left as ingredients, since a recipe can write both as its own words. A plain walk, no
/// regular expression (ADR 0057).
/// </remarks>
internal static class SchemaOrgIngredients
{
    private const string Markers = "*#";

    /// <summary>
    /// The ingredients of the value in order, each with the heading above it. A heading gives its name to the lines
    /// after it, and a heading with no words, or with nothing under it, adds nothing.
    /// </summary>
    public static IEnumerable<RecipeIngredient> Read(JsonElement? value)
    {
        string? section = null;
        foreach (var line in SchemaOrgValues.Texts(value, keepTags: true))
        {
            // A heading sets the section for what follows; a line with no words left once its tags are out is nothing.
            if (HeadingName(line) is { } name)
                section = name.Length > 0 ? name : section;
            else if (SchemaOrgValues.WithoutTags(line).Trim() is { Length: > 0 } text)
                yield return new RecipeIngredient(section, text);
        }
    }

    /// <summary>The heading's name as the page wrote it (empty if none), or null when the line isn't one.</summary>
    private static string? HeadingName(string line) =>
        TagHeading(line) ?? MarkerHeading(line);

    /// <summary>
    /// The text inside a line that is wholly one <c>&lt;h1&gt;</c> to <c>&lt;h6&gt;</c> (any case, any attributes),
    /// without its tags, or null when the line is anything else.
    /// </summary>
    private static string? TagHeading(string line)
    {
        if (line.Length < 9 || line[0] != '<' || char.ToLowerInvariant(line[1]) != 'h') return null;
        if (line[2] is < '1' or > '6') return null;

        // The opening tag runs to its first '>', and the line must end with the matching closing tag.
        var opened = line.IndexOf('>');
        var closing = $"</h{line[2]}>";
        if (opened < 0 || !line.EndsWith(closing, StringComparison.OrdinalIgnoreCase)) return null;
        if (opened + 1 > line.Length - closing.Length) return null;

        return SchemaOrgValues.WithoutTags(line[(opened + 1)..^closing.Length]).Trim();
    }

    /// <summary>
    /// The name after two or more leading stars or hashes (<c>***Filling</c>, <c>### Sauce</c>), without the ones
    /// that close it (<c>**Crust**</c>), or null when the line starts with fewer. A marker left inside means the stars
    /// only make some words bold (<c>**2 eggs** (large)</c>), and that line is an ingredient.
    /// </summary>
    private static string? MarkerHeading(string line)
    {
        var name = line.Trim(Markers.ToCharArray());
        var markers = line.Length - line.TrimStart(Markers.ToCharArray()).Length;
        if (markers < 2 || name.IndexOfAny(Markers.ToCharArray()) >= 0) return null;

        return SchemaOrgValues.WithoutTags(name).Trim();
    }
}

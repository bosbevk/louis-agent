using System.Text.Json;
using LouisAgent.Core.Tools.Web.StructuredData;

namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// Reads the recipe a page publishes for machines: the <c>Recipe</c> in its JSON-LD (schema.org), with the name, the
/// yield, the times, the ingredients, the steps, the picture and the author. If the page has none, or one you couldn't
/// cook from, you get null.
/// </summary>
/// <remarks>
/// Data Mapper (Fowler, PoEAA). Sites write the same recipe in many shapes (a text or a list, a step or a section, an
/// author as a name or an object), and this is the one class that knows them, so the rest of the pack only ever
/// sees a <see cref="Recipe"/>. Without it each tool would carry its own idea of the page's shape. The field reads
/// are switch expressions and guard clauses, never conditionals inside conditionals (ADR 0051).
/// With a guard on what it hands back (Louis's pick, F35.1): some pages mark up a Recipe with its ingredients and
/// steps left empty, or a list page as a Recipe. The check lives here so that no caller can hand the cook an empty
/// recipe, and such a page is read as text like any page without the data.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0067-a-pages-own-data-before-its-text.md">ADR 0067: a page's own data before its text</seealso>
public static class SchemaOrgRecipe
{
    private const string RecipeType = "Recipe";

    /// <summary>
    /// The first recipe in the page's data, as long as it's whole. The source is where the page came from, for
    /// resolving a relative picture address and to say where the recipe is from.
    /// </summary>
    public static Recipe? Read(string html, Uri source)
    {
        var blocks = JsonLdBlocks.In(html);
        try
        {
            // The first block that holds a recipe wins; the rest of the page's data is about something else.
            return blocks.Select(b => FindRecipe(b.RootElement)).FirstOrDefault(e => e is not null) is { } entity
                ? WholeOrNull(Map(entity, source))
                : null;
        }
        finally
        {
            // The blocks own their memory, so we give it back once the recipe is a plain record.
            foreach (var block in blocks) block.Dispose();
        }
    }

    /// <summary>You can't cook from a recipe with no name, nothing to put in or nothing to do.</summary>
    private static Recipe? WholeOrNull(Recipe recipe) =>
        recipe is { Name.Length: > 0, Ingredients.Count: > 0, Steps.Count: > 0 } ? recipe : null;

    /// <summary>
    /// Walks a block to the first thing that is a recipe: through a list, and into a <c>@graph</c> (Yoast and Rank
    /// Math put the recipe there beside the page's other entities).
    /// </summary>
    private static JsonElement? FindRecipe(JsonElement node) => node.ValueKind switch
    {
        JsonValueKind.Array => node.EnumerateArray().Select(FindRecipe).FirstOrDefault(e => e is not null),
        JsonValueKind.Object when IsRecipe(node) => node,
        JsonValueKind.Object when node.TryGetProperty("@graph", out var graph) => FindRecipe(graph),
        _ => null
    };

    /// <summary>Whether the entity's <c>@type</c> is Recipe, or a list of types that includes it.</summary>
    private static bool IsRecipe(JsonElement entity) =>
        SchemaOrgValues.Texts(SchemaOrgValues.Property(entity, "@type")).Contains(RecipeType);

    /// <summary>Builds the record from the entity, one reader per field.</summary>
    private static Recipe Map(JsonElement entity, Uri source) => new(
        Name: SchemaOrgValues.Text(SchemaOrgValues.Property(entity, "name")) ?? "",
        Yield: SchemaOrgValues.Texts(SchemaOrgValues.Property(entity, "recipeYield")).LastOrDefault(),
        PrepTime: SchemaOrgValues.Duration(SchemaOrgValues.Property(entity, "prepTime")),
        CookTime: SchemaOrgValues.Duration(SchemaOrgValues.Property(entity, "cookTime")),
        TotalTime: SchemaOrgValues.Duration(SchemaOrgValues.Property(entity, "totalTime")),
        Ingredients: SchemaOrgIngredients.Read(SchemaOrgValues.Property(entity, "recipeIngredient")).ToList(),
        Steps: Steps(SchemaOrgValues.Property(entity, "recipeInstructions"), null).ToList(),
        Image: SchemaOrgValues.Address(
            SchemaOrgValues.Text(SchemaOrgValues.Property(entity, "image"), objectKey: "url"), source),
        Author: SchemaOrgValues.Text(SchemaOrgValues.Property(entity, "author"), objectKey: "name"),
        Source: source);

    /// <summary>
    /// The steps of the instructions: a plain text is one step, a list is its items' steps, and a section's name goes
    /// on every step inside it.
    /// </summary>
    private static IEnumerable<RecipeStep> Steps(JsonElement? node, string? section) => node switch
    {
        { ValueKind: JsonValueKind.String } v => SchemaOrgValues.Text(v) is { } text
            ? [new RecipeStep(section, text)]
            : [],
        { ValueKind: JsonValueKind.Array } v => v.EnumerateArray().SelectMany(item => Steps(item, section)),
        { ValueKind: JsonValueKind.Object } v => StepsOfObject(v, section),
        _ => []
    };

    /// <summary>
    /// A step object (a <c>HowToStep</c> with its <c>text</c>) or a section (a <c>HowToSection</c> with a <c>name</c>
    /// and an <c>itemListElement</c> of steps). A step with no words is no step.
    /// </summary>
    private static IEnumerable<RecipeStep> StepsOfObject(JsonElement node, string? section)
    {
        // A section holds its steps in itemListElement; its name is the group they belong to.
        if (SchemaOrgValues.Property(node, "itemListElement") is { } items)
            return Steps(items, SchemaOrgValues.Text(SchemaOrgValues.Property(node, "name")) ?? section);

        return SchemaOrgValues.Text(SchemaOrgValues.Property(node, "text")) is { } text
            ? [new RecipeStep(section, text)]
            : [];
    }
}

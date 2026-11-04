namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// One thing that goes into a recipe, and the part of the recipe it belongs to ("Filling", "Topping") when the page
/// groups its ingredients under headings.
/// </summary>
/// <remarks>
/// A record because it is a plain value: two lines with the same words are the same ingredient. It has the same shape
/// as <see cref="RecipeStep"/>, so the print can treat a heading among the ingredients like one among the steps.
/// </remarks>
/// <param name="Section">
/// What the page calls the group, in its own words (a line such as <c>***Filling</c> or
/// <c>&lt;h3&gt;Sauce&lt;/h3&gt;</c> gives "Filling" or "Sauce"); null when the page has no headings above this line.
/// </param>
/// <param name="Text">The ingredient as the page wrote it, with no HTML left in it.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecipeIngredient(string? Section, string Text);

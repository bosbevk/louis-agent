namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// One thing to do in a recipe, and the part of the recipe it belongs to ("Sauce", "Pudding") when the page groups
/// its steps.
/// </summary>
/// <remarks>
/// A record because it is a plain value: two steps with the same words are the same step.
/// </remarks>
/// <param name="Section">
/// What the page calls the group (a <c>HowToSection</c>'s <c>name</c>); null when it has none.
/// </param>
/// <param name="Text">What to do, as the page wrote it (a <c>HowToStep</c>'s <c>text</c>).</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RecipeStep(string? Section, string Text);

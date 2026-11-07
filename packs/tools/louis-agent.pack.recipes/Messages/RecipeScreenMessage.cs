using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>What the library's screens say on the admin pages (ADR 0009), for a person, never the model.</summary>
public enum RecipeScreenMessage
{
    [Description("Recipe library")]
    UsageTitle,

    [Description("What the recipe library did each day, from the cook's reads and searches: recipes kept, hits of ours shown in a search, recipes served from us, and reads we couldn't serve. Days are UTC.")]
    UsageDescription,

    [Description("Day")]
    Day,

    [Description("All days")]
    AllDays,

    [Description("Kept")]
    Kept,

    [Description("Offered")]
    Offered,

    [Description("Served")]
    Served,

    [Description("Failed")]
    Failed,

    [Description("Recipes")]
    RecipesTitle,

    [Description("Every recipe the library keeps: search them, open one to change its words, or remove one.")]
    RecipesDescription,

    [Description("Name")]
    Name,

    [Description("Site")]
    Site,

    [Description("Yield")]
    Yield,

    [Description("Read")]
    ReadAt,

    [Description("Edited")]
    Edited,

    [Description("Prep, in minutes")]
    PrepMinutes,

    [Description("Cooking, in minutes")]
    CookMinutes,

    [Description("Total, in minutes")]
    TotalMinutes,

    [Description("Photo's address")]
    Image,

    [Description("Author")]
    Author,

    [Description("Ingredients")]
    Ingredients,

    [Description("Steps")]
    Steps,

    [Description("Section")]
    Section,

    [Description("Line")]
    Line,

    [Description("Page")]
    Page,

    [Description("A recipe needs a name.")]
    NameRequired,

    [Description("A recipe needs at least one ingredient.")]
    IngredientRequired,

    [Description("A recipe needs at least one step.")]
    StepRequired,

    [Description("This needs a number of minutes, or nothing.")]
    MinutesRefused,

    [Description("This needs a full web address, or nothing.")]
    AddressRefused,

    [Description("Not saved: see the fields marked.")]
    NotSaved,

    [Description("Saved, and marked as edited.")]
    Saved,

    [Description("That recipe isn't in the library any more.")]
    RecipeGone,

    [Description("Remove")]
    Remove,

    [Description("Remove this recipe, and keep its page out of the library?")]
    RemoveConfirm,

    [Description("Removed. Its page stays out until it's cleared, and the open feed can still serve it for up to five minutes.")]
    Removed,

    [Description("Excluded pages")]
    ExcludedTitle,

    [Description("The pages nobody opens again: those with no recipe, and those removed by hand.")]
    ExcludedDescription,

    [Description("Excluded")]
    ExcludedAt,

    [Description("Clear")]
    ClearPage,

    [Description("Clear this page, so the next read looks at it again?")]
    ClearConfirm,

    [Description("Cleared. The next read looks at it again, and keeps it if it's a recipe.")]
    Cleared,

    [Description("That page isn't on the list any more.")]
    PageNotListed
}

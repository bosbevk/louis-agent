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
    Failed
}

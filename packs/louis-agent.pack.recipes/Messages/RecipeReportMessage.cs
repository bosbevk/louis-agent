using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>What the library's report says on the admin page (ADR 0009), for a person, never the model.</summary>
public enum RecipeReportMessage
{
    [Description("What the recipe library did each day, from the cook's reads and searches: recipes kept, hits of ours shown in a search, recipes served from us, and reads we couldn't serve. Days are UTC.")]
    Description,

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

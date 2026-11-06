namespace LouisAgent.Composition.Packs.Screens;

/// <summary>What a screen is: its name, its title and sentence for the page, its columns, and whether it can be searched.</summary>
/// <param name="Name">"recipe-library": lowercase letters, digits and dashes, unique in the catalogue.</param>
/// <param name="Title">The heading on its page and its link in the sidebar.</param>
/// <param name="Description">What it shows, one sentence.</param>
/// <param name="Columns">Its columns, in the order they read.</param>
/// <param name="Searchable">Whether the page offers a search box.</param>
public sealed record ScreenSpec(
    string Name,
    string Title,
    string Description,
    IReadOnlyList<ScreenColumn> Columns,
    bool Searchable);

using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Recipes.Web;

/// <summary>The recipe library's screen: the plain table, with a line above it saying how much was served.</summary>
/// <remarks>
/// Decorator (Gang of Four), in Razor: it wraps the framework's table and adds one line through its slot, so the
/// search, the paging and the cells stay in one place. Writing a table of its own would copy all of them for one
/// sentence.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/decorator">Decorator (Refactoring.Guru)</seealso>
public partial class RecipeLibraryView : IScreenView
{
    /// <inheritdoc />
    [Parameter, EditorRequired] public ScreenReply Screen { get; set; } = null!;
}

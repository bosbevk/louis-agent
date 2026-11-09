using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Movies;

/// <summary>A film's frame: the film as it is, and one control to pause it and play it again.</summary>
/// <remarks>
/// Decorator, in Razor (Gang of Four): the frame wraps a film and adds the pausing, so a film stays markup and CSS
/// alone, its golden and its exported WebP untouched; the pause is one class on the frame, which its stylesheet
/// turns into every animation under the film standing still.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md">ADR 0066: the site is static Razor, and its films are CSS</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/decorator">Decorator (Refactoring.Guru)</seealso>
public partial class FilmFrame
{
    private bool _paused;

    /// <summary>The film.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Pauses the film, or plays it again.</summary>
    private void Toggle() => _paused = !_paused;
}

using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Composition.Packs.Screens;

/// <summary>
/// A screen a pack offers whoever runs it, as plain data: what it's called, its columns, and its rows. One generic page
/// draws any of them behind the admin key, so a new screen needs no change to the API or to a site.
/// </summary>
/// <remarks>
/// Plugin (Fowler, PoEAA), the same seam as <see cref="Imports.IDataImport"/>: core names the shape and each pack plugs
/// its own in, so the API never references a pack (ADR 0027). Described, not drawn: a pack holds no web code, and when a
/// description can't say what's wanted, a real component from the pack's web project replaces it by name. Reading is
/// free of failure, as a tool's is (ADR 0005): a store that's down gives an empty page.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public interface IDataScreen
{
    /// <summary>What the screen is called and what it shows.</summary>
    ScreenSpec Spec { get; }

    /// <summary>One page of its rows, or an empty page when its store can't be read.</summary>
    Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Does one of the actions its <see cref="Spec"/> names, on this row if it's a row's. A screen is read-only until it
    /// says otherwise, so one that offers nothing writes none of this.
    /// </summary>
    Task<ScreenAnswer> ActAsync(string action, string? rowId, CancellationToken cancellationToken) =>
        Task.FromResult(ScreenAnswer.NotOffered);

    /// <summary>One row as a form, if its <see cref="Spec"/> says rows can be edited; null for no such row.</summary>
    Task<ScreenRecord?> ReadRowAsync(string rowId, CancellationToken cancellationToken) =>
        Task.FromResult<ScreenRecord?>(null);

    /// <summary>Saves a form, or says which fields it refused.</summary>
    Task<ScreenAnswer> SaveRowAsync(string rowId, ScreenValues values, CancellationToken cancellationToken) =>
        Task.FromResult(ScreenAnswer.NotOffered);
}

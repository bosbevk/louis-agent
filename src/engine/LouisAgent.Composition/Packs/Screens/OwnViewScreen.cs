using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Composition.Packs.Screens;

/// <summary>
/// A screen with no rows to describe, such as a git panel: the pack names it, and its own view draws everything. With
/// no columns it can't be drawn any other way, so a site that doesn't carry the view doesn't list it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed class OwnViewScreen(string name, string title, string description) : IDataScreen
{
    /// <inheritdoc />
    public ScreenSpec Spec { get; } = new(name, title, description, [], Searchable: false);

    /// <inheritdoc />
    public Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(ScreenPage.Empty);
}
